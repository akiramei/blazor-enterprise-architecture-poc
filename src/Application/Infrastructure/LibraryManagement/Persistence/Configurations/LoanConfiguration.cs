using Domain.LibraryManagement.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Infrastructure.LibraryManagement.Persistence.Configurations;

/// <summary>
/// Loan エンティティのEF Core設定
/// </summary>
public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(l => l.Id);
        builder.Ignore(l => l.MemberId);
        builder.Ignore(l => l.BookCopyId);
        builder.Ignore(l => l.LoanDate);
        builder.Ignore(l => l.DueDate);
        builder.Ignore(l => l.ReturnDate);
        builder.Ignore(l => l.IsReturned);
        builder.Ignore(l => l.IsOverdue);
        builder.Ignore(l => l.DomainEvents);

        // 主キー
        builder.HasKey("_id");

        // LoanId（Typed ID）
        builder.Property<LoanId>("_id")
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new LoanId(value))
            .IsRequired();

        // MemberId（外部キー）
        builder.Property<MemberId>("_memberId")
            .HasColumnName("MemberId")
            .HasConversion(
                id => id.Value,
                value => new MemberId(value))
            .IsRequired();

        // BookCopyId（外部キー）
        builder.Property<BookCopyId>("_bookCopyId")
            .HasColumnName("BookCopyId")
            .HasConversion(
                id => id.Value,
                value => new BookCopyId(value))
            .IsRequired();

        // 貸出日
        builder.Property<DateTime>("_loanDate")
            .HasColumnName("LoanDate")
            .IsRequired();

        // 返却期限日
        builder.Property<DateTime>("_dueDate")
            .HasColumnName("DueDate")
            .IsRequired();

        // 返却日（nullable）
        builder.Property<DateTime?>("_returnDate")
            .HasColumnName("ReturnDate");

        // インデックス
        builder.HasIndex("_memberId");
        builder.HasIndex("_bookCopyId");
    }
}
