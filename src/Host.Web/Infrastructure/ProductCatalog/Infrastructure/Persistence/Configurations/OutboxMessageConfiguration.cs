using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Outbox;

namespace ProductCatalog.Shared.Infrastructure.Persistence.Configurations;

/// <summary>
/// OutboxMessage エンティティのEF Core設定
///
/// 【トランザクショナルOutboxパターン】
///
/// 責務:
/// - Outbox物理同居の設定（論理所有=Platform）
/// - ディスパッチ最適化のためのインデックス設定
/// - BC衝突回避のためのテーブル命名規約
///
/// 設計:
/// - テーブル名: pc_OutboxMessages（BC接頭辞で衝突回避）
/// - インデックス: ProcessedOnUtc（未処理検索用）
/// - 論理所有: Platform（ディスパッチはPlatformBackgroundServiceが実施）
/// - 物理同居: ProductCatalog BC（書き込み時の原子性確保）
///
/// AI実装時の注意:
/// - 将来、複数BCがある場合は各BCに同じパターンを適用
/// - テーブル名は必ずBC接頭辞を付与（例: pc_, order_, inventory_）
/// - インデックスは未処理メッセージの高速検索を最適化
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // テーブル名（BC接頭辞で衝突回避）
        builder.ToTable("pc_OutboxMessages");

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
        // WHERE ProcessedOnUtc IS NULL ORDER BY OccurredOnUtc
        builder.HasIndex(o => new { o.ProcessedOnUtc, o.OccurredOnUtc })
            .HasDatabaseName("IX_pc_OutboxMessages_ProcessedOn_OccurredOn");
    }
}
