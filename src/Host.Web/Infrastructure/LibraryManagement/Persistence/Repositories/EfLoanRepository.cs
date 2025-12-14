using Application.Infrastructure.LibraryManagement.Persistence;
using Domain.LibraryManagement.Loans;
using Microsoft.EntityFrameworkCore;

namespace Application.Infrastructure.LibraryManagement.Persistence.Repositories;

public sealed class EfLoanRepository : ILoanRepository
{
    private readonly LibraryManagementDbContext _context;

    public EfLoanRepository(LibraryManagementDbContext context)
    {
        _context = context;
    }

    public Task<Member?> GetMemberByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        => _context.Members.FirstOrDefaultAsync(m => m.Barcode == barcode, cancellationToken);

    public Task<BookCopy?> GetBookCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        => _context.BookCopies.FirstOrDefaultAsync(b => b.Barcode == barcode, cancellationToken);

    public async Task SaveLoanAsync(Loan loan, CancellationToken cancellationToken = default)
        => await _context.Loans.AddAsync(loan, cancellationToken);

    public Task UpdateMemberAsync(Member member, CancellationToken cancellationToken = default)
    {
        _context.Members.Update(member);
        return Task.CompletedTask;
    }

    public Task UpdateBookCopyAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        _context.BookCopies.Update(bookCopy);
        return Task.CompletedTask;
    }
}

