using Shared.Domain.Idempotency;

namespace Shared.Application.Interfaces;

/// <summary>
/// 冪等性レコードのストア
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 冪等性レコードを取得
    /// </summary>
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 冪等性レコードを保存
    /// </summary>
    Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default);

    /// <summary>
    /// 有効期限切れのレコードをクリーンアップ
    /// </summary>
    Task CleanupExpiredAsync(TimeSpan expiration, CancellationToken ct = default);
}
