namespace Domain.LibraryManagement.Reservations;

/// <summary>
/// 予約リポジトリインターフェース
/// </summary>
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> GetActiveByMemberIdAsync(
        Loans.MemberId memberId,
        CancellationToken ct = default);

    Task<Reservation?> GetByMemberAndBookAsync(
        Loans.MemberId memberId,
        Loans.BookCopyId bookCopyId,
        CancellationToken ct = default);

    Task<int> GetQueuePositionAsync(
        Loans.BookCopyId bookCopyId,
        CancellationToken ct = default);

    Task AddAsync(Reservation reservation, CancellationToken ct = default);

    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
}
