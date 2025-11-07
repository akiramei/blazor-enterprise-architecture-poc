namespace Shared.Abstractions.Platform;

/// <summary>
/// 冪等性保証のポートインターフェース
///
/// 【Infrastructure.Platform】
/// このインターフェースは技術的関心事（リクエスト重複検知）を抽象化します。
/// ビジネスロジックはこのポートを通じて、具体的な実装（RDB/Redis/Memcached）に依存しません。
///
/// 実装例:
/// - Shared.Infrastructure.Platform.IdempotencyStore (InMemory実装)
/// - Shared.Infrastructure.Platform.RedisIdempotencyStore (Redis実装)
/// - Shared.Infrastructure.Platform.SqlIdempotencyStore (SQL実装)
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// リクエストが既に処理済みかチェック
    /// </summary>
    Task<bool> IsProcessedAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// リクエストを処理済みとして記録
    /// </summary>
    Task MarkAsProcessedAsync(string requestId, object? result, CancellationToken cancellationToken = default);

    /// <summary>
    /// 処理済みリクエストの結果を取得
    /// </summary>
    Task<object?> GetResultAsync(string requestId, CancellationToken cancellationToken = default);
}
