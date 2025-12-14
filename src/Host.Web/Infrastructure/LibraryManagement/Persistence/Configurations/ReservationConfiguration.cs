using Domain.LibraryManagement.Loans;
using Domain.LibraryManagement.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Infrastructure.LibraryManagement.Persistence.Configurations;

/// <summary>
/// Reservation エンティティのEF Core設定
/// </summary>
public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        // DomainEvents は永続化しない
        builder.Ignore(r => r.DomainEvents);

        // 主キー（Reservation は public property の Id を使用）
        builder.HasKey(r => r.Id);

        // ReservationId（Typed ID）
        builder.Property(r => r.Id)
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new ReservationId(value))
            .IsRequired();

        // MemberId（外部キー）
        builder.Property(r => r.MemberId)
            .HasColumnName("MemberId")
            .HasConversion(
                id => id.Value,
                value => new MemberId(value))
            .IsRequired();

        // BookCopyId（外部キー）
        builder.Property(r => r.BookCopyId)
            .HasColumnName("BookCopyId")
            .HasConversion(
                id => id.Value,
                value => new BookCopyId(value))
            .IsRequired();

        // 予約日時
        builder.Property(r => r.ReservedAt)
            .HasColumnName("ReservedAt")
            .IsRequired();

        // 待機順位
        builder.Property(r => r.QueuePosition)
            .HasColumnName("QueuePosition")
            .IsRequired();

        // ステータス（Enum → int）
        builder.Property(r => r.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        // インデックス
        builder.HasIndex(r => r.MemberId);
        builder.HasIndex(r => r.BookCopyId);
        builder.HasIndex(r => new { r.BookCopyId, r.QueuePosition });
    }
}
