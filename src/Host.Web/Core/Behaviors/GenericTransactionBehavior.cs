using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Domain.Outbox;
using Shared.Kernel;

namespace Application.Core.Behaviors;

/// <summary>
/// BC非依存の汎用トランザクション管理Behavior
///
/// 【目的】
/// - BC別のTransactionBehavior実装を排除 (DRY原則)
/// - Command名からBCを自動推論してDbContextを動的解決
/// - すべてのBCで一貫したトランザクション管理
///
/// 【動作】
/// 1. Commandの名前空間からBC名を抽出 (例: "Application.Features.PurchaseManagement.SubmitPurchaseRequestCommand" → "PurchaseManagement")
/// 2. BC名から対応するDbContext型を解決 (IServiceProvider経由)
/// 3. トランザクション開始 → Handler実行 → Commit/Rollback
/// 4. ドメインイベントをOutboxに保存 (Transactional Outbox Pattern)
///
/// 【Transactional Outbox Pattern】
/// - ビジネスエンティティ更新とOutbox保存を単一トランザクションで実行
/// - ドメインイベントをOutboxテーブルに永続化
/// - 非同期で別プロセスがOutboxを読み取りイベント配信
/// - → ビジネスロジックとイベント配信の原子性確保
///
/// 【BC別DbContext登録方法】
/// <code>
/// // Boundaries/Host/DependencyInjection/DatabaseServiceExtensions.cs
/// services.AddDbContext&lt;PurchaseManagementDbContext&gt;(...);
/// services.AddDbContext&lt;ProductCatalogDbContext&gt;(...);
///
/// // BC→DbContext型のマッピングを登録
/// services.AddSingleton&lt;IBoundedContextResolver&gt;(sp =>
///     new BoundedContextResolver(new Dictionary&lt;string, Type&gt;
///     {
///         ["PurchaseManagement"] = typeof(PurchaseManagementDbContext),
///         ["ProductCatalog"] = typeof(ProductCatalogDbContext)
///     }));
/// </code>
///
/// 【工業製品化への貢献】
/// - BC追加時にTransactionBehavior実装不要
/// - トランザクション戦略を一箇所に集約
/// - Outbox実装もBC非依存で再利用
/// </summary>
/// <typeparam name="TRequest">リクエスト型 (ICommand)</typeparam>
/// <typeparam name="TResponse">レスポンス型 (Result)</typeparam>
public sealed class GenericTransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBoundedContextResolver _bcResolver;
    private readonly ILogger<GenericTransactionBehavior<TRequest, TResponse>> _logger;

    public GenericTransactionBehavior(
        IServiceProvider serviceProvider,
        IBoundedContextResolver bcResolver,
        ILogger<GenericTransactionBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider;
        _bcResolver = bcResolver;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Commandの名前空間からBC名を抽出
        var boundedContext = ExtractBoundedContext(request);

        // BC特定できない場合はスキップ (Behaviorをバイパス)
        if (string.IsNullOrEmpty(boundedContext))
        {
            _logger.LogWarning(
                "BC特定不可のためトランザクションスキップ: {RequestType}",
                typeof(TRequest).FullName);
            return await next();
        }

        // 2. BC別DbContextを解決
        var dbContext = ResolveDbContext(boundedContext);

        if (dbContext == null)
        {
            _logger.LogWarning(
                "DbContext未登録のためトランザクションスキップ: BC={BoundedContext}",
                boundedContext);
            return await next();
        }

        // 3. ネストトランザクション防止
        if (dbContext.Database.CurrentTransaction != null)
        {
            _logger.LogDebug("既存トランザクション検出 - ネストスキップ: BC={BoundedContext}", boundedContext);
            return await next();
        }

        var commandName = typeof(TRequest).Name;
        _logger.LogDebug("トランザクション開始: {CommandName}, BC={BoundedContext}", commandName, boundedContext);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4. Handler実行
            var response = await next();

            if (response.IsSuccess)
            {
                // 5. ドメインイベント→Outbox変換
                await DispatchDomainEventsAsync(dbContext, cancellationToken);

                // 6. Commit
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogDebug("トランザクションコミット: {CommandName}", commandName);
            }
            else
            {
                // ビジネスルール違反 → Rollback
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug(
                    "トランザクションロールバック(ビジネスルール違反): {CommandName}, Error={Error}",
                    commandName,
                    response.Error);
            }

            return response;
        }
        catch (Exception ex)
        {
            // 例外発生 → Rollback
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "トランザクションロールバック(例外): {CommandName}", commandName);
            throw;
        }
    }

    /// <summary>
    /// Commandの名前空間からBC名を抽出
    /// 例: "Application.Features.PurchaseManagement.SubmitPurchaseRequestCommand" → "PurchaseManagement"
    /// </summary>
    private string ExtractBoundedContext(TRequest request)
    {
        var fullName = request.GetType().FullName ?? string.Empty;

        // パターン: "Application.Features.{BC}.{Feature}Command"
        var parts = fullName.Split('.');
        var featureIndex = Array.IndexOf(parts, "Features");

        if (featureIndex >= 0 && featureIndex + 1 < parts.Length)
        {
            return parts[featureIndex + 1]; // "Features"の次がBC名
        }

        // 旧パターン: "{BC}.Features.{Feature}.Application.{Feature}Command"
        // 例: "PurchaseManagement.Features.SubmitPurchaseRequest.Application.SubmitPurchaseRequestCommand"
        if (parts.Length > 0 && parts[0] != "Application")
        {
            return parts[0]; // 最初がBC名
        }

        return string.Empty;
    }

    /// <summary>
    /// BC名から対応するDbContextを解決
    /// IBoundedContextResolver経由で型マッピングを取得
    /// </summary>
    private DbContext? ResolveDbContext(string boundedContext)
    {
        var dbContextType = _bcResolver.ResolveDbContextType(boundedContext);

        if (dbContextType == null)
        {
            return null;
        }

        return _serviceProvider.GetService(dbContextType) as DbContext;
    }

    /// <summary>
    /// ドメインイベントをOutboxに変換して永続化
    /// Transactional Outbox Pattern実装
    /// </summary>
    private async Task DispatchDomainEventsAsync(DbContext dbContext, CancellationToken ct)
    {
        var domainEntities = dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.GetDomainEvents().Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();

        // ドメインイベントをクリア (重複配信防止)
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // Outbox Pattern: ドメインイベントをOutboxテーブルに保存
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var eventTypeName = eventType.Name;
            var eventContent = JsonSerializer.Serialize(domainEvent, eventType, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var outboxMessage = OutboxMessage.Create(eventTypeName, eventContent);

            // OutboxMessagesプロパティを動的に取得
            var outboxSet = dbContext.GetType()
                .GetProperty("OutboxMessages")
                ?.GetValue(dbContext) as DbSet<OutboxMessage>;

            if (outboxSet != null)
            {
                await outboxSet.AddAsync(outboxMessage, ct);

                _logger.LogInformation(
                    "ドメインイベントをOutboxに保存: {EventType} [OutboxMessageId: {OutboxMessageId}]",
                    eventTypeName,
                    outboxMessage.Id);
            }
            else
            {
                _logger.LogWarning(
                    "OutboxMessagesプロパティが見つかりません: DbContext={DbContextType}",
                    dbContext.GetType().Name);
            }
        }
    }
}

/// <summary>
/// BC名からDbContext型を解決するインターフェース
/// </summary>
public interface IBoundedContextResolver
{
    /// <summary>
    /// BC名から対応するDbContext型を取得
    /// </summary>
    /// <param name="boundedContext">BC名 (例: "PurchaseManagement")</param>
    /// <returns>DbContext型 (未登録の場合はnull)</returns>
    Type? ResolveDbContextType(string boundedContext);
}

/// <summary>
/// BC→DbContext型のマッピングを保持する実装
/// </summary>
public sealed class BoundedContextResolver : IBoundedContextResolver
{
    private readonly Dictionary<string, Type> _mappings;

    public BoundedContextResolver(Dictionary<string, Type> mappings)
    {
        _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
    }

    public Type? ResolveDbContextType(string boundedContext)
    {
        return _mappings.TryGetValue(boundedContext, out var type) ? type : null;
    }
}
