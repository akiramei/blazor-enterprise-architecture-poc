namespace Shared.Abstractions.Platform;

/// <summary>
/// 監査ログ保存のポートインターフェース
///
/// 【Infrastructure.Platform】
/// このインターフェースは技術的関心事（監査ログ永続化）を抽象化します。
/// ビジネスロジックはこのポートを通じて、具体的な実装（RDB/NoSQL/SIEM）に依存しません。
///
/// 実装例:
/// - Shared.Infrastructure.Platform.AuditLogStore (EF Core実装)
/// - Shared.Infrastructure.Platform.ElasticsearchAuditLogStore (Elasticsearch実装)
/// </summary>
public interface IAuditLogStore
{
    /// <summary>
    /// 監査ログを保存
    /// トランザクション内で呼び出されることを想定（オプショナル）
    /// </summary>
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 監査ログを検索（管理画面用）
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> SearchAsync(
        DateTime? from,
        DateTime? to,
        string? userId,
        string? action,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 監査ログエントリ
/// プラットフォーム層のデータモデル（ドメインモデルではない）
/// </summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
