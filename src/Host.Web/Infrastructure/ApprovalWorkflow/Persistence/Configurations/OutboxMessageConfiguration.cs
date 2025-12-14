using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Outbox;

namespace ApprovalWorkflow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Outboxメッセージ設定（物理同居）
///
/// 【パターン: Transactional Outbox】
///
/// 責務:
/// - ドメインイベントをOutboxMessageとして保存
/// - BCと同一トランザクションで原子性を保証
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // BC固有のプレフィックスでテーブル名衝突を回避
        builder.ToTable("aw_OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.OccurredOnUtc)
            .IsRequired();

        builder.Property(m => m.ProcessedOnUtc);

        builder.Property(m => m.Error)
            .HasMaxLength(4000);

        // 未処理メッセージ検索用インデックス
        builder.HasIndex(m => m.ProcessedOnUtc)
            .HasDatabaseName("IX_aw_OutboxMessages_ProcessedOnUtc");
    }
}
