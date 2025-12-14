using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Outbox;

namespace Application.Infrastructure.LibraryManagement.Persistence.Configurations;

/// <summary>
/// OutboxMessage エンティティのEF Core設定（LibraryManagement BC用）
///
/// 【トランザクショナルOutboxパターン】
/// - テーブル名: lib_OutboxMessages（BC接頭辞で衝突回避）
/// - 論理所有: Platform（ディスパッチはPlatformBackgroundServiceが実施）
/// - 物理同居: LibraryManagement BC（書き込み時の原子性確保）
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // テーブル名（BC接頭辞で衝突回避）
        builder.ToTable("lib_OutboxMessages");

        // 主キー
        builder.HasKey(o => o.Id);

        // プロパティ設定
        builder.Property(o => o.Id)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Content)
            .IsRequired();

        builder.Property(o => o.OccurredOnUtc)
            .IsRequired();

        builder.Property(o => o.ProcessedOnUtc)
            .IsRequired(false);

        builder.Property(o => o.Error)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(o => o.RetryCount)
            .IsRequired();

        // インデックス（未処理メッセージの高速検索用）
        builder.HasIndex(o => new { o.ProcessedOnUtc, o.OccurredOnUtc })
            .HasDatabaseName("IX_lib_OutboxMessages_ProcessedOn_OccurredOn");
    }
}
