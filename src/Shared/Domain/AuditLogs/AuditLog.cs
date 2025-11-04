namespace Shared.Domain.AuditLogs;

/// <summary>
/// 監査ログエンティティ
///
/// 【パターン: 監査ログ（Audit Log）】
///
/// 使用シナリオ:
/// - ユーザーアクションの記録
/// - データ変更履歴の追跡
/// - コンプライアンス対応
/// - セキュリティ監査
///
/// 設計ガイド:
/// - すべての重要なCommand実行を自動記録
/// - 変更前後のデータをJSON形式で保存
/// - ユーザー、テナント、リクエスト情報を保持
/// - 削除不可（イミュータブルなログ）
///
/// 実装詳細:
/// - AuditLogBehaviorによる自動記録
/// - IAuditableCommandマーカーインターフェースで記録対象を指定
/// - Correlation IDによる分散トレーシング対応
/// </summary>
public class AuditLog
{
    /// <summary>
    /// 監査ログID
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// 実行ユーザーID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 実行ユーザー名
    /// </summary>
    public string UserName { get; private set; }

    /// <summary>
    /// テナントID（マルチテナント対応）
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// アクション名（例: CreateProduct, UpdateProduct, DeleteProduct）
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// エンティティ型（例: Product, Order）
    /// </summary>
    public string EntityType { get; private set; }

    /// <summary>
    /// エンティティID
    /// </summary>
    public string EntityId { get; private set; }

    /// <summary>
    /// 変更前の値（JSON形式）
    /// </summary>
    public string? OldValues { get; private set; }

    /// <summary>
    /// 変更後の値（JSON形式）
    /// </summary>
    public string? NewValues { get; private set; }

    /// <summary>
    /// Correlation ID（分散トレーシング）
    /// </summary>
    public string CorrelationId { get; private set; }

    /// <summary>
    /// Request ID
    /// </summary>
    public Guid RequestId { get; private set; }

    /// <summary>
    /// リクエストパス（例: /api/products）
    /// </summary>
    public string? RequestPath { get; private set; }

    /// <summary>
    /// HTTPメソッド（例: POST, PUT, DELETE）
    /// </summary>
    public string? HttpMethod { get; private set; }

    /// <summary>
    /// クライアントIPアドレス
    /// </summary>
    public string? ClientIpAddress { get; private set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// 実行日時（UTC）
    /// </summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>
    /// プライベートコンストラクタ（EF Core用）
    /// </summary>
    private AuditLog()
    {
        Id = Guid.Empty;
        UserName = string.Empty;
        Action = string.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        CorrelationId = string.Empty;
    }

    /// <summary>
    /// ファクトリメソッド - 監査ログを作成
    /// </summary>
    public static AuditLog Create(
        Guid userId,
        string userName,
        Guid? tenantId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string correlationId,
        Guid requestId,
        string? requestPath = null,
        string? httpMethod = null,
        string? clientIpAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName cannot be null or empty", nameof(userName));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType cannot be null or empty", nameof(entityType));

        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("EntityId cannot be null or empty", nameof(entityId));

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId cannot be null or empty", nameof(correlationId));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            CorrelationId = correlationId,
            RequestId = requestId,
            RequestPath = requestPath,
            HttpMethod = httpMethod,
            ClientIpAddress = clientIpAddress,
            UserAgent = userAgent,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
