using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.AuditLogs;

namespace Shared.Infrastructure.Persistence.Configurations;

/// <summary>
/// AuditLog エンティティのEF Core設定
///
/// 【パターン: Fluent API設定】
///
/// 設計方針:
/// - テーブル名: AuditLogs
/// - インデックス: EntityType+EntityId, UserId, TimestampUtc（検索最適化）
/// - 文字列長制限: パフォーマンスとストレージ最適化
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // 主キー
        builder.HasKey(x => x.Id);

        // プロパティ設定
        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TenantId)
            .IsRequired(false);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OldValues)
            .IsRequired(false)
            .HasColumnType("text"); // PostgreSQL: text, SQL Server: nvarchar(max)

        builder.Property(x => x.NewValues)
            .IsRequired(false)
            .HasColumnType("text");

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.RequestId)
            .IsRequired();

        builder.Property(x => x.RequestPath)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.HttpMethod)
            .IsRequired(false)
            .HasMaxLength(10);

        builder.Property(x => x.ClientIpAddress)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(x => x.UserAgent)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        // インデックス（検索パフォーマンス最適化）
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(x => x.TimestampUtc)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");
    }
}
