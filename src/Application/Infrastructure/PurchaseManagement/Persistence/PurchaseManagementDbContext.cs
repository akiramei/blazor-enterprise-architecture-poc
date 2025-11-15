using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Domain.PurchaseManagement;
using Domain.PurchaseManagement.PurchaseRequests;
using Shared.Application.Interfaces;
using Shared.Domain.Outbox;
using Shared.Kernel;

namespace PurchaseManagement.Infrastructure.Persistence;

/// <summary>
/// PurchaseManagement Bounded ContextのDbContext
///
/// 【VSA + Infrastructure.Platform パターン】
///
/// 責務:
/// - PurchaseManagement BCのビジネスエンティティ管理（PurchaseRequest、ApprovalStep、PurchaseRequestItem）
/// - トランザクショナルOutbox（物理同居、論理所有=Platform）
///
/// 設計原則:
/// - Bounded Context単位でのトランザクション境界
/// - ビジネスドメインロジックに集中
/// - Outbox物理同居により単一トランザクションでの原子性確保
///
/// Outbox設計:
/// - **論理所有**: Platform（ディスパッチはPlatform責務）
/// - **物理同居**: BC DbContext（書き込み時の原子性確保）
/// - **命名**: pm_OutboxMessages（BC接頭辞で衝突回避）
///
/// VSA構造:
/// - PurchaseManagement/Infrastructure/Persistence（このプロジェクト）
/// - Shared.Infrastructure.Platform（技術的関心事、読み出し/配信）
///
/// トランザクション戦略:
/// - 単一BC内の操作: PurchaseManagementDbContext単独
/// - クロスカッティング操作: TransactionScope or Saga pattern
/// </summary>
public sealed class PurchaseManagementDbContext : DbContext
{
    private readonly IAppContext _appContext;
    private readonly IWebHostEnvironment _environment;

    public PurchaseManagementDbContext(
        DbContextOptions<PurchaseManagementDbContext> options,
        IAppContext appContext,
        IWebHostEnvironment environment) : base(options)
    {
        _appContext = appContext;
        _environment = environment;
    }

    // ビジネスエンティティ
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestAttachment> PurchaseRequestAttachments => Set<PurchaseRequestAttachment>();

    // Outbox（物理同居、論理所有=Platform）
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Objectsをエンティティとして扱わないように設定
        modelBuilder.Ignore<PurchaseRequestNumber>();
        modelBuilder.Ignore<Money>();

        // DomainEventは別テーブルで管理されるため無視
        modelBuilder.Ignore<DomainEvent>();

        // PurchaseManagement BC のConfiguration適用
        // PurchaseManagement.Infrastructure.Persistence アセンブリから設定を読み込む
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchaseManagementDbContext).Assembly);

        // Global Query Filter: マルチテナント分離（Test環境では無効化）
        if (!_environment.IsEnvironment("Test"))
        {
            ApplyGlobalFilters(modelBuilder);
        }
    }

    /// <summary>
    /// Global Query Filterの適用
    /// </summary>
    /// <remarks>
    /// IMultiTenantインターフェースを実装する全てのエンティティに対して、
    /// 現在のユーザーのTenantIdでフィルタリングを自動適用します。
    ///
    /// フィルタの動作:
    /// - TenantIdがnullの場合（未認証ユーザー）: 全てのデータを除外
    /// - TenantIdが設定されている場合: そのテナントのデータのみ取得
    ///
    /// 全テナントのデータを取得する場合（管理者機能など）:
    /// context.PurchaseRequests.IgnoreQueryFilters().ToList()
    /// </remarks>
    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        // IMultiTenantを実装する全てのエンティティに対してテナント分離フィルタを適用
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                // 動的にメソッドを呼び出してフィルタを適用
                var method = typeof(PurchaseManagementDbContext)
                    .GetMethod(nameof(SetMultiTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    /// <summary>
    /// マルチテナントフィルタを設定（ジェネリックメソッド）
    ///
    /// 重要: _appContext.TenantIdを直接式ツリーで参照することで、
    /// クエリ実行時に動的にTenantIdを評価します。
    /// ローカル変数にキャプチャすると、DbContext作成時の値で固定されてしまいます。
    ///
    /// 【SECURITY FIX】
    /// _appContext.TenantId == null の場合、全データを返すのではなく、
    /// 全データをブロックするように変更（false を返す条件に変更）。
    /// 未認証ユーザーや設定ミスのコンテキストでデータ漏洩を防止。
    ///
    /// 修正前: _appContext.TenantId == null || e.TenantId == _appContext.TenantId.Value
    ///   → null の場合に true となり、全テナントのデータが見える（脆弱性）
    ///
    /// 修正後: _appContext.TenantId != null && e.TenantId == _appContext.TenantId.Value
    ///   → null の場合に false となり、全データをブロック（安全）
    /// </summary>
    private void SetMultiTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMultiTenant
    {
        // SECURITY: TenantId が null の場合は全データをブロック
        // NOTE: Nullable<Guid> を直接比較することで EF Core がSQL に正しく変換できる
        //       .Value を使うと null チェックがあってもランタイムエラーになる場合がある
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => _appContext.TenantId.HasValue && e.TenantId == _appContext.TenantId);
    }
}
