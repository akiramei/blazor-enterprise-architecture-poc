using Microsoft.EntityFrameworkCore;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using Shared.Domain.Outbox;

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
    public PurchaseManagementDbContext(DbContextOptions<PurchaseManagementDbContext> options) : base(options)
    {
    }

    // ビジネスエンティティ
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();

    // Outbox（物理同居、論理所有=Platform）
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Objectsをエンティティとして扱わないように設定
        modelBuilder.Ignore<PurchaseRequestNumber>();
        modelBuilder.Ignore<ProductCatalog.Shared.Domain.Products.Money>();

        // PurchaseManagement BC のConfiguration適用
        // PurchaseManagement.Infrastructure.Persistence アセンブリから設定を読み込む
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchaseManagementDbContext).Assembly);
    }
}
