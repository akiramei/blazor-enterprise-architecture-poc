using Domain.LibraryManagement.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Infrastructure.LibraryManagement.Persistence.Configurations;

/// <summary>
/// BookCopy エンティティのEF Core設定
/// </summary>
public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(b => b.Id);
        builder.Ignore(b => b.Title);
        builder.Ignore(b => b.Barcode);
        builder.Ignore(b => b.Status);
        builder.Ignore(b => b.IsAvailableForLoan);
        builder.Ignore(b => b.IsReferenceOnly);
        builder.Ignore(b => b.DomainEvents);

        // 主キー
        builder.HasKey("_id");

        // BookCopyId（Typed ID）
        builder.Property<BookCopyId>("_id")
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new BookCopyId(value))
            .IsRequired();

        // タイトル
        builder.Property<string>("_title")
            .HasColumnName("Title")
            .HasMaxLength(500)
            .IsRequired();

        // バーコード
        builder.Property<string>("_barcode")
            .HasColumnName("Barcode")
            .HasMaxLength(50)
            .IsRequired();

        // ステータス（Enum → int）
        builder.Property<BookCopyStatus>("_status")
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        // インデックス
        builder.HasIndex("_barcode").IsUnique();
    }
}
