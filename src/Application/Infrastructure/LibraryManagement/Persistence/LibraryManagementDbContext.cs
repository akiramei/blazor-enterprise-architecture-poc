using Domain.LibraryManagement.Loans;
using Domain.LibraryManagement.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Domain.Outbox;
using Shared.Infrastructure.Diagnostics;

namespace Application.Infrastructure.LibraryManagement.Persistence;

/// <summary>
/// LibraryManagement Bounded ContextのDbContext
///
/// 責務:
/// - 図書館BCのビジネスエンティティ（Member, BookCopy, Loan, Reservation）
/// - トランザクショナルOutbox（物理同居）
/// </summary>
public sealed class LibraryManagementDbContext : DbContext
{
    private readonly ILogger<LibraryManagementDbContext> _logger;

    public LibraryManagementDbContext(
        DbContextOptions<LibraryManagementDbContext> options,
        ILogger<LibraryManagementDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogEntityState(this);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<MemberId>();
        modelBuilder.Ignore<BookCopyId>();
        modelBuilder.Ignore<LoanId>();
        modelBuilder.Ignore<ReservationId>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryManagementDbContext).Assembly);
    }
}

