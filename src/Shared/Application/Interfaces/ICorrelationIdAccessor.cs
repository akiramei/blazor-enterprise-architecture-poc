namespace Shared.Application.Interfaces;

/// <summary>
/// CorrelationID アクセサー（分散トレーシング用）
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// 現在のリクエストの CorrelationID を取得
    /// </summary>
    string? CorrelationId { get; }
}
