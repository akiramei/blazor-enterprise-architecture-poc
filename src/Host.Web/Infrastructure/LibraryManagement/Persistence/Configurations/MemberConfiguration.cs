using Domain.LibraryManagement.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Infrastructure.LibraryManagement.Persistence.Configurations;

/// <summary>
/// Member エンティティのEF Core設定
/// </summary>
public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(m => m.Id);
        builder.Ignore(m => m.Name);
        builder.Ignore(m => m.Barcode);
        builder.Ignore(m => m.CurrentLoanCount);
        builder.Ignore(m => m.IsActive);
        builder.Ignore(m => m.DomainEvents);

        // 主キー
        builder.HasKey("_id");

        // MemberId（Typed ID）
        builder.Property<MemberId>("_id")
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new MemberId(value))
            .IsRequired();

        // 名前
        builder.Property<string>("_name")
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        // バーコード
        builder.Property<string>("_barcode")
            .HasColumnName("Barcode")
            .HasMaxLength(50)
            .IsRequired();

        // 現在の貸出数
        builder.Property<int>("_currentLoanCount")
            .HasColumnName("CurrentLoanCount")
            .IsRequired();

        // 有効フラグ
        builder.Property<bool>("_isActive")
            .HasColumnName("IsActive")
            .IsRequired();

        // インデックス
        builder.HasIndex("_barcode").IsUnique();
    }
}
