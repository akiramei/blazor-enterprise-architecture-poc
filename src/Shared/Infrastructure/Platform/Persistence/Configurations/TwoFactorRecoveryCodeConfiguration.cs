using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Identity;

namespace Shared.Infrastructure.Platform.Persistence.Configurations;

/// <summary>
/// TwoFactorRecoveryCode エンティティのEF Core設定
/// </summary>
public sealed class TwoFactorRecoveryCodeConfiguration : IEntityTypeConfiguration<TwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorRecoveryCode> builder)
    {
        builder.ToTable("TwoFactorRecoveryCodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.IsUsed)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // インデックス: ユーザーIDと未使用コードの検索を高速化
        builder.HasIndex(x => new { x.UserId, x.IsUsed })
            .HasDatabaseName("IX_TwoFactorRecoveryCodes_UserId_IsUsed");
    }
}
