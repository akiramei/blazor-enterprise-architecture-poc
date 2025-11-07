using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products.Events;

/// <summary>
/// 商品作成ドメインイベント
///
/// 商品が新規作成された際に発行されるイベント。
/// このイベントはトランザクショナルOutboxパターンを通じて、
/// 他のBounded Contextや外部システムに配信される。
///
/// 用途例:
/// - 在庫管理システムへの新商品通知
/// - 検索インデックスの更新
/// - 監査ログの記録
/// - リアルタイム通知（SignalR）
/// </summary>
public sealed record ProductCreatedDomainEvent(
    ProductId ProductId,
    string ProductName,
    Money Price,
    int InitialStock
) : DomainEvent;
