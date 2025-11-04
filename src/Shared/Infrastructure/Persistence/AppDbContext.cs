using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.AuditLogs;
using Shared.Domain.Identity;
using Shared.Domain.Outbox;
using ProductCatalog.Shared.Domain.Products;
using Shared.Infrastructure.Authentication;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// アプリケーションDbContext（ASP.NET Core Identity対応）
///
/// 【⚠️ LEGACY】この DbContext は互換性のために残されています
///
/// 新しい構造（Infrastructure.Platform パターン）:
/// - ProductCatalogDbContext: ビジネスエンティティ（Product）を管理
///   → ProductCatalog.Shared.Infrastructure.Persistence
///
/// - PlatformDbContext: 技術的関心事（Outbox、AuditLog、Identity）を管理
///   → Shared.Infrastructure.Platform.Persistence
///
/// 移行ガイド:
/// 1. ビジネスロジック（Product操作）: AppDbContext → ProductCatalogDbContext
/// 2. 技術的操作（Outbox、AuditLog）: AppDbContext → PlatformDbContext
/// 3. テストコード: AppDbContext → 適切なDbContextに変更
///
/// このクラスは次のフェーズで削除予定です
/// </summary>
[Obsolete("Use ProductCatalogDbContext for business entities or PlatformDbContext for platform concerns")]
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Value Objectsをエンティティとして扱わないように設定
        // （EF CoreがAggregateRoot<TId>のジェネリック型パラメータをエンティティと誤認識するのを防ぐ）
        modelBuilder.Ignore<ProductId>();
        modelBuilder.Ignore<ProductImageId>();

        // Identityテーブル名のプレフィックスを設定（オプション）
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName != null && tableName.StartsWith("AspNet"))
            {
                entityType.SetTableName(tableName.Replace("AspNet", "Identity"));
            }
        }

        // Configurationを適用
        // VSA構造: 複数のアセンブリから設定を読み込む
        // 1. Shared.Infrastructure.Persistence (AuditLog, OutboxMessage, RefreshToken)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 2. ProductCatalog.Shared.Infrastructure.Persistence (Product)
        // Product型を使用して、確実にアセンブリを取得
        var productType = typeof(Product);
        var productAssemblyName = productType.Assembly.GetName().Name!;

        // "ProductCatalog.Shared.Domain.Products" → "ProductCatalog.Shared.Infrastructure.Persistence" に変換
        var persistenceAssemblyName = productAssemblyName.Replace("Shared.Domain.Products", "Shared.Infrastructure.Persistence");

        var productPersistenceAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == persistenceAssemblyName);

        if (productPersistenceAssembly != null)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(productPersistenceAssembly);
        }
        else
        {
            // アセンブリが見つからない場合はログに記録（開発時のデバッグ用）
            // 本番環境では通常、すべてのアセンブリが読み込まれているはず
            System.Diagnostics.Debug.WriteLine(
                $"Warning: {persistenceAssemblyName} assembly not found. " +
                $"Entity configurations may not be applied correctly.");
        }
    }
}
