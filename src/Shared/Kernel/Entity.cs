namespace Shared.Kernel;

/// <summary>
/// エンティティの基底クラス
/// 識別子による同一性を持つ
/// </summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// ドメインイベント一覧（読み取り専用）
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// ドメインイベントを取得
    /// </summary>
    public IReadOnlyList<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// ドメインイベントを追加
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// ドメインイベントをクリア
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
