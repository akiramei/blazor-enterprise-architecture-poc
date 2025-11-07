using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.AuditLogs;
using Shared.Domain.Identity;
using Shared.Domain.Outbox;
using Shared.Infrastructure.Authentication;

namespace Shared.Infrastructure.Platform.Persistence;

/// <summary>
/// プラットフォーム層のDbContext
///
/// 【Infrastructure.Platform パターン】
///
/// 責務:
/// - 技術的関心事のエンティティ管理（Outbox、AuditLog、Identity、RefreshToken）
/// - ビジネスドメインエンティティは含まない（Product等は別のDbContext）
///
/// 設計原則:
/// - ビジネスとインフラストラクチャの分離
/// - 技術的トランザクション境界の明確化
/// - 独立したマイグレーション管理
///
/// VSA構造:
/// - Shared.Infrastructure.Platform（技術的関心事）
/// - ProductCatalog.Shared.Infrastructure.Persistence（ビジネス関心事）
/// </summary>
public sealed class PlatformDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    // プラットフォームエンティティ
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identityテーブル名のプレフィックスを設定（オプション）
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName != null && tableName.StartsWith("AspNet"))
            {
                entityType.SetTableName(tableName.Replace("AspNet", "Identity"));
            }
        }

        // プラットフォーム層のConfiguration適用
        // Shared.Infrastructure.Platform アセンブリから設定を読み込む
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);

        // Shared.Infrastructure（旧構造）からも読み込み（互換性のため）
        var sharedInfraAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Shared.Infrastructure");

        if (sharedInfraAssembly != null)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(sharedInfraAssembly);
        }
    }
}
