using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Outbox;

namespace PurchaseManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// OutboxMessage エンティティのEF Core設定（PurchaseManagement BC用）
///
/// 【パターン: Transactional Outbox】
///
/// 責務:
/// - OutboxMessageのテーブルマッピング
/// - BC接頭辞によるテーブル名の衝突回避（pm_OutboxMessages）
/// - インデックス設定（Processed、ProcessedOnUtc）
///
/// 設計原則:
/// - **論理所有**: Platform（ディスパッチはPlatform責務）
/// - **物理同居**: PurchaseManagement BC（書き込み時の原子性確保）
/// - **命名規約**: pm_OutboxMessages（BC接頭辞 + OutboxMessages）
///
/// AI実装時の注意:
/// - テーブル名はBC接頭辞を付ける（pm_OutboxMessages）
/// - Processedフィールドにインデックスを設定（未処理メッセージの高速検索）
/// - ProcessedOnUtcフィールドにインデックスを設定（処理履歴の検索）
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // BC接頭辞を付けたテーブル名（pm = PurchaseManagement）
        builder.ToTable("pm_OutboxMessages");

        // 主キー
        builder.HasKey(o => o.Id);

        // Type
        builder.Property(o => o.Type)
            .HasColumnName("Type")
            .HasMaxLength(500)
            .IsRequired();

        // Content（JSON）
        builder.Property(o => o.Content)
            .HasColumnName("Content")
            .IsRequired();

        // OccurredOnUtc
        builder.Property(o => o.OccurredOnUtc)
            .HasColumnName("OccurredOnUtc")
            .IsRequired();

        // ProcessedOnUtc
        builder.Property(o => o.ProcessedOnUtc)
            .HasColumnName("ProcessedOnUtc");

        // Error
        builder.Property(o => o.Error)
            .HasColumnName("Error");

        // RetryCount
        builder.Property(o => o.RetryCount)
            .HasColumnName("RetryCount")
            .IsRequired();

        // インデックス: 処理日時による検索（未処理メッセージの高速検索）
        builder.HasIndex(o => o.ProcessedOnUtc)
            .HasDatabaseName("IX_pm_OutboxMessages_ProcessedOnUtc");
    }
}
