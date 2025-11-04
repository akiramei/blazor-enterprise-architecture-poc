using Microsoft.EntityFrameworkCore;
using Shared.Application.Interfaces;
using Shared.Domain.AuditLogs;

namespace Shared.Infrastructure.Persistence.Repositories;

/// <summary>
/// 監査ログリポジトリ実装（EF Core）
///
/// 【パターン: リポジトリパターン実装】
///
/// 実装方針:
/// - 監査ログは追記のみ（更新・削除不可）
/// - 検索機能は必要に応じて拡張可能
/// - パフォーマンスを考慮したクエリ最適化
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        // Note: SaveChangesはTransactionBehaviorで実行される
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.TimestampUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.TimestampUtc >= startUtc && x.TimestampUtc <= endUtc)
            .OrderByDescending(x => x.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
