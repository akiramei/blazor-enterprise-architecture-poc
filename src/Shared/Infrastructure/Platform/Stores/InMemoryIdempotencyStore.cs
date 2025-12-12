using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;
using Shared.Domain.Idempotency;

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
public sealed class InMemoryIdempotencyStore : IIdempotencyStore, Shared.Application.Interfaces.IIdempotencyStore
{
    private const string LegacyCommandType = "Legacy";
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
        var record = IdempotencyRecord.Create(
            key: requestId,
            commandType: LegacyCommandType,
            result: result ?? (object)"null");

        _store[requestId] = record;

        _logger.LogDebug("リクエストを処理済みとしてマークしました。[RequestId: {RequestId}]", requestId);

        return Task.CompletedTask;
    }

    public Task<object?> GetResultAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(requestId, out var record))
        {
            _logger.LogDebug("処理済みリクエストの結果を取得しました。[RequestId: {RequestId}]", requestId);

            // Return the JSON string as the result
            // Note: The caller is responsible for deserializing if needed
            object? result = record.ResultJson;
            return Task.FromResult<object?>(result);
        }

        _logger.LogDebug("処理済みリクエストの結果が見つかりません。[RequestId: {RequestId}]", requestId);
        return Task.FromResult<object?>(null);
    }

    // Shared.Application.Interfaces.IIdempotencyStore implementation
    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var record))
        {
            _logger.LogDebug("冪等性レコードを取得しました。[Key: {Key}]", key);
            return Task.FromResult<IdempotencyRecord?>(record);
        }

        _logger.LogDebug("冪等性レコードが見つかりません。[Key: {Key}]", key);
        return Task.FromResult<IdempotencyRecord?>(null);
    }

    public Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default)
    {
        _store[record.Key] = record;

        _logger.LogDebug("冪等性レコードを保存しました。[Key: {Key}]", record.Key);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 有効期限切れレコードのクリーンアップ
    /// （バックグラウンドサービスから定期実行）
    /// </summary>
    public Task CleanupExpiredAsync(TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - expiration;

        var expiredKeys = _store
            .Where(kvp => kvp.Value.CreatedAt < cutoffTime)
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
}
