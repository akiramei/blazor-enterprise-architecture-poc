using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Outbox;

namespace Shared.Infrastructure.Persistence.Configurations;

/// <summary>
/// OutboxMessage エンティティの設定
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc)
            .IsRequired(false);

        builder.Property(x => x.Error)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        // インデックス（未処理メッセージの高速検索用）
        builder.HasIndex(x => x.ProcessedOnUtc);
    }
}
