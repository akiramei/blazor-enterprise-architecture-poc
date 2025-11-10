using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductCatalog.Shared.Domain.Products;
using Shared.Domain.Outbox;
using Shared.Infrastructure.Diagnostics;

namespace ProductCatalog.Shared.Infrastructure.Persistence;

/// <summary>
/// ProductCatalog Bounded ContextのDbContext
///
/// 【VSA + Infrastructure.Platform パターン】
///
/// 責務:
/// - ProductCatalog BCのビジネスエンティティ管理（Product、ProductImage）
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
/// - **命名**: pc_OutboxMessages（BC接頭辞で衝突回避）
///
/// VSA構造:
/// - ProductCatalog/Shared/Infrastructure/Persistence（このプロジェクト）
/// - Shared.Infrastructure.Platform（技術的関心事、読み出し/配信）
///
/// トランザクション戦略:
/// - 単一BC内の操作: ProductCatalogDbContext単独
/// - クロスカッティング操作: TransactionScope or Saga pattern
/// </summary>
public sealed class ProductCatalogDbContext : DbContext
{
    private readonly ILogger<ProductCatalogDbContext> _logger;

    public ProductCatalogDbContext(
        DbContextOptions<ProductCatalogDbContext> options,
        ILogger<ProductCatalogDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    // ビジネスエンティティ
    public DbSet<Product> Products => Set<Product>();

    // Outbox（物理同居、論理所有=Platform）
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 診断ログ: EF Coreの変更追跡状態を出力
        _logger.LogEntityState(this);

        // SQLiteではrowversionが使えないため、Versionを手動でインクリメント
        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("Version").CurrentValue = 1L;
            }
            else if (entry.State == EntityState.Modified)
            {
                var currentVersion = entry.Property("Version").OriginalValue as long? ?? 0L;
                entry.Property("Version").CurrentValue = currentVersion + 1;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Objectsをエンティティとして扱わないように設定
        // （EF CoreがAggregateRoot<TId>のジェネリック型パラメータをエンティティと誤認識するのを防ぐ）
        modelBuilder.Ignore<ProductId>();
        modelBuilder.Ignore<ProductImageId>();

        // ProductCatalog BC のConfiguration適用
        // ProductCatalog.Shared.Infrastructure.Persistence アセンブリから設定を読み込む
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);
    }
}
