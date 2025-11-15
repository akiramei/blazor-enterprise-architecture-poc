using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;
using Shared.Domain.Outbox;

namespace ProductCatalog.Shared.Infrastructure.Persistence.Behaviors;

/// <summary>
/// トランザクション管理のPipeline Behavior（Command専用）
///
/// 【トランザクショナルOutboxパターン】
///
/// 責務:
/// - ビジネスエンティティ更新とOutbox保存の原子性確保
/// - ドメインイベントをOutboxに変換して永続化
/// - トランザクション管理（コミット/ロールバック）
///
/// 設計:
/// - ProductCatalogDbContext使用（Product + Outbox物理同居）
/// - 単一トランザクションで原子性確保
/// - ネストトランザクション回避
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly ProductCatalogDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        ProductCatalogDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ネストされたトランザクションを防ぐため、既存トランザクションがあればスキップ
        if (_context.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var commandName = typeof(TRequest).Name;

        _logger.LogDebug("トランザクション開始: {CommandName}", commandName);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                // ドメインイベントをディスパッチ
                await DispatchDomainEventsAsync(cancellationToken);

                // Commit
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogDebug("トランザクションコミット: {CommandName}", commandName);
            }
            else
            {
                // ビジネスルール違反の場合もロールバック
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("トランザクションロールバック(ビジネスルール違反): {CommandName}", commandName);
            }

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "トランザクションロールバック(例外): {CommandName}", commandName);
            throw;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.GetDomainEvents().Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // Outbox Pattern: ドメインイベントをOutboxテーブルに保存
        // トランザクション内で保存するため、確実に配信される
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var eventTypeName = eventType.Name;
            var eventContent = JsonSerializer.Serialize(domainEvent, eventType, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var outboxMessage = OutboxMessage.Create(eventTypeName, eventContent);
            await _context.OutboxMessages.AddAsync(outboxMessage, ct);

            _logger.LogInformation(
                "ドメインイベントをOutboxに保存: {EventType} [OutboxMessageId: {OutboxMessageId}]",
                eventType,
                outboxMessage.Id);
        }
    }
}
