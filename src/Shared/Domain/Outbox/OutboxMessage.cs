namespace Shared.Domain.Outbox;

/// <summary>
/// Outbox パターン用のメッセージエンティティ
/// ドメインイベントを確実に外部システムに配信するため、トランザクション内でDBに保存
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// メッセージID（一意識別子）
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// メッセージタイプ（イベント名、クラス名など）
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// メッセージ内容（JSON形式）
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// 発生日時（UTC）
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// 処理日時（UTC、nullの場合は未処理）
    /// </summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>
    /// エラー情報（処理失敗時）
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// リトライ回数
    /// </summary>
    public int RetryCount { get; private set; }

    // EF Core用のプライベートコンストラクタ
    private OutboxMessage()
    {
        Type = string.Empty;
        Content = string.Empty;
    }

    /// <summary>
    /// OutboxMessage を作成
    /// </summary>
    public static OutboxMessage Create(string type, string content)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOnUtc = DateTime.UtcNow,
            ProcessedOnUtc = null,
            Error = null,
            RetryCount = 0
        };
    }

    /// <summary>
    /// 処理完了としてマーク
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Error = null;
    }

    /// <summary>
    /// 処理失敗としてマーク
    /// </summary>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }

    /// <summary>
    /// リトライ可能かどうか
    /// </summary>
    public bool CanRetry(int maxRetryCount = 3)
    {
        return RetryCount < maxRetryCount;
    }
}
