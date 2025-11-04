using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;

namespace Shared.Infrastructure.Platform.Stores;

/// <summary>
/// InMemory 冪等性ストア
///
/// 【Infrastructure.Platform パターン】
///
/// 責務:
/// - リクエストIDの重複検知
/// - 処理結果のキャッシュ
/// - 有効期限管理
///
/// 設計原則:
/// - ポート（IIdempotencyStore）の具体実装
/// - インメモリ実装（開発・テスト用）
/// - 本番環境ではRedis実装に置き換え推奨
///
/// 注意:
/// - スケールアウト環境では動作しない（単一インスタンスのみ）
/// - アプリケーション再起動でデータ消失
/// - 本番環境ではRedisIdempotencyStoreを使用すること
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();
    private readonly ILogger<InMemoryIdempotencyStore> _logger;

    public InMemoryIdempotencyStore(ILogger<InMemoryIdempotencyStore> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsProcessedAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var isProcessed = _store.ContainsKey(requestId);

        if (isProcessed)
        {
            _logger.LogDebug("リクエストは既に処理済みです。[RequestId: {RequestId}]", requestId);
        }

        return Task.FromResult(isProcessed);
    }

    public Task MarkAsProcessedAsync(string requestId, object? result, CancellationToken cancellationToken = default)
    {
        var serializedResult = result != null
            ? JsonSerializer.Serialize(result)
            : null;

        var record = new IdempotencyRecord
        {
            RequestId = requestId,
            Result = serializedResult,
            ProcessedAt = DateTime.UtcNow
        };

        _store[requestId] = record;

        _logger.LogDebug("リクエストを処理済みとしてマークしました。[RequestId: {RequestId}]", requestId);

        return Task.CompletedTask;
    }

    public Task<object?> GetResultAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(requestId, out var record))
        {
            _logger.LogDebug("処理済みリクエストの結果を取得しました。[RequestId: {RequestId}]", requestId);

            // 簡略化: 結果をJSON文字列として返す
            // 実際の実装では型情報も保存して復元する必要がある
            object? result = record.Result;
            return Task.FromResult(result);
        }

        _logger.LogDebug("処理済みリクエストの結果が見つかりません。[RequestId: {RequestId}]", requestId);
        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// 有効期限切れレコードのクリーンアップ
    /// （バックグラウンドサービスから定期実行）
    /// </summary>
    public Task CleanupExpiredAsync(TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - expiration;

        var expiredKeys = _store
            .Where(kvp => kvp.Value.ProcessedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _store.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("有効期限切れの冪等性レコードを {Count} 件削除しました。", expiredKeys.Count);
        }

        return Task.CompletedTask;
    }

    private sealed record IdempotencyRecord
    {
        public required string RequestId { get; init; }
        public string? Result { get; init; }
        public DateTime ProcessedAt { get; init; }
    }
}
