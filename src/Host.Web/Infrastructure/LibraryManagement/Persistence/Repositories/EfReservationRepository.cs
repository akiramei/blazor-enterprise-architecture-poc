using Application.Infrastructure.LibraryManagement.Persistence;
using Domain.LibraryManagement.Loans;
using Domain.LibraryManagement.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Application.Infrastructure.LibraryManagement.Persistence.Repositories;

public sealed class EfReservationRepository : IReservationRepository
{
    private readonly LibraryManagementDbContext _context;

    public EfReservationRepository(LibraryManagementDbContext context)
    {
        _context = context;
    }

    public Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken ct = default)
        => _context.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reservation>> GetActiveByMemberIdAsync(MemberId memberId, CancellationToken ct = default)
        => await _context.Reservations
            .Where(r => r.MemberId == memberId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

    public Task<Reservation?> GetByMemberAndBookAsync(MemberId memberId, BookCopyId bookCopyId, CancellationToken ct = default)
        => _context.Reservations.FirstOrDefaultAsync(
            r => r.MemberId == memberId && r.BookCopyId == bookCopyId && r.Status == ReservationStatus.Active, ct);

    public async Task<int> GetQueuePositionAsync(BookCopyId bookCopyId, CancellationToken ct = default)
        => await _context.Reservations.CountAsync(
            r => r.BookCopyId == bookCopyId && r.Status == ReservationStatus.Active, ct);

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
        => await _context.Reservations.AddAsync(reservation, ct);

    public Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Update(reservation);
        return Task.CompletedTask;
    }
}

