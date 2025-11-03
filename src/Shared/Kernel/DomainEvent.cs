namespace ProductCatalog.Domain.Common;

/// <summary>
/// ドメインイベントの基底クラス
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// イベントID
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// イベント発生日時（UTC）
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
