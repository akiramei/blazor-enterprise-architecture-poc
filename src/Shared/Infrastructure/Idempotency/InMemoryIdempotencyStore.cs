using System.Collections.Concurrent;
using Shared.Domain.Idempotency;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Idempotency;

/// <summary>
/// InMemory による冪等性レコードストア（実証実験用）
/// 本番環境では Redis や データベーステーブル を使用
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new();

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        _store.TryGetValue(key, out var record);
        return Task.FromResult(record);
    }

    public Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default)
    {
        _store[record.Key] = record;
        return Task.CompletedTask;
    }

    public Task CleanupExpiredAsync(TimeSpan expiration, CancellationToken ct = default)
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

        return Task.CompletedTask;
    }
}
