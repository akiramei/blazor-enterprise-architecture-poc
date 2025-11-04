using Shared.Domain.AuditLogs;

namespace Shared.Application.Interfaces;

/// <summary>
/// 監査ログリポジトリインターフェース
///
/// 【パターン: リポジトリパターン】
///
/// 使用シナリオ:
/// - 監査ログの保存
/// - 監査ログの検索・取得
///
/// 実装ガイド:
/// - AuditLogBehaviorから自動的に呼び出される
/// - 監査ログは削除不可（追記のみ）
/// - 検索機能は必要に応じて拡張
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 監査ログを追加
    /// </summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 監査ログをIDで取得
    /// </summary>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 特定エンティティの監査ログを取得
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザーの監査ログを取得
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 期間指定で監査ログを取得
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
