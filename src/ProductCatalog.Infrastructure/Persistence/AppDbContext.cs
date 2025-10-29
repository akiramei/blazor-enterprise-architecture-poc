using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Identity;
using ProductCatalog.Domain.Outbox;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence;

/// <summary>
/// アプリケーションDbContext（ASP.NET Core Identity対応）
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
