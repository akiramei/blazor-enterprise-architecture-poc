using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Identity;

namespace Shared.Infrastructure.Platform.Persistence.Configurations;

/// <summary>
/// ApplicationUser エンティティのEF Core設定
/// 2FA関連プロパティの設定
/// </summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // 既存のIdentityUserテーブルに追加のカラムを設定

        builder.Property(x => x.IsTwoFactorEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.TwoFactorSecretKey)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.TwoFactorEnabledAt)
            .IsRequired(false);

        builder.Property(x => x.TwoFactorRecoveryCodesRemaining)
            .IsRequired()
            .HasDefaultValue(0);

        // インデックス: 2FA有効ユーザーの検索を高速化
        builder.HasIndex(x => x.IsTwoFactorEnabled)
            .HasDatabaseName("IX_IdentityUsers_IsTwoFactorEnabled");
    }
}
