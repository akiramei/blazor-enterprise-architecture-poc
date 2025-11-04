using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;
using Shared.Domain.AuditLogs;
using Shared.Infrastructure.Platform.Persistence;

namespace Shared.Infrastructure.Platform.Stores;

/// <summary>
/// AuditLog Store の EF Core 実装
///
/// 【Infrastructure.Platform パターン】
///
/// 責務:
/// - 監査ログの永続化（PlatformDbContext使用）
/// - 監査ログの検索（管理画面用）
///
/// 設計原則:
/// - ポート（IAuditLogStore）の具体実装
/// - ビジネスロジックから技術的実装を分離
/// - PlatformDbContextを使用（技術的関心事専用）
/// - 監査ログは追記のみ（更新・削除不可）
/// </summary>
public sealed class AuditLogStore : IAuditLogStore
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<AuditLogStore> _logger;

    public AuditLogStore(
        PlatformDbContext context,
        ILogger<AuditLogStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        // Shared.Abstractions.Platform.AuditLogEntry → Shared.Domain.AuditLogs.AuditLog に変換
        var auditLog = AuditLog.Create(
            Guid.Parse(entry.UserId),
            entry.UserId, // UserIdをUserNameとして使用（簡略化）
            null, // TenantId
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.OldValues,
            entry.NewValues,
            Guid.NewGuid().ToString(), // CorrelationId（簡略化）
            entry.Id,
            null, // RequestPath
            null, // HttpMethod
            entry.IpAddress,
            entry.UserAgent);

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

        _logger.LogDebug(
            "監査ログを追加しました。[Action: {Action}] [EntityType: {EntityType}] [EntityId: {EntityId}]",
            entry.Action,
            entry.EntityType,
            entry.EntityId);
    }

    public async Task<IEnumerable<AuditLogEntry>> SearchAsync(
        DateTime? from,
        DateTime? to,
        string? userId,
        string? action,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        // フィルタリング
        if (from.HasValue)
        {
            query = query.Where(a => a.TimestampUtc >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.TimestampUtc <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                query = query.Where(a => a.UserId == userGuid);
            }
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        // ページング
        var logs = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("監査ログを {Count} 件取得しました。", logs.Count);

        // Shared.Domain.AuditLogs.AuditLog → Shared.Abstractions.Platform.AuditLogEntry に変換
        return logs.Select(a => new AuditLogEntry
        {
            Id = a.Id,
            UserId = a.UserId.ToString(),
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            Timestamp = a.TimestampUtc,
            IpAddress = a.ClientIpAddress ?? string.Empty,
            UserAgent = a.UserAgent ?? string.Empty
        });
    }
}
