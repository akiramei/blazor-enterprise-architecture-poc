namespace Shared.Abstractions.Platform;

/// <summary>
/// Transactional Outbox パターンのポートインターフェース
///
/// 【Infrastructure.Platform】
/// このインターフェースは技術的関心事（イベント永続化）を抽象化します。
/// ビジネスロジックはこのポートを通じて、具体的な実装（RDB/NoSQL/MessageBroker）に依存しません。
///
/// 実装例:
/// - Shared.Infrastructure.Platform.OutboxStore (EF Core実装)
/// - Shared.Infrastructure.Platform.MongoDbOutboxStore (MongoDB実装)
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Outboxメッセージを保存
    /// トランザクション内で呼び出されることを想定
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 未処理のメッセージを取得（バッチ処理用）
    /// </summary>
    Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// メッセージを処理済みとしてマーク
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// エラーメッセージを記録
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outboxメッセージエンティティ
/// プラットフォーム層のデータモデル（ドメインモデルではない）
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
