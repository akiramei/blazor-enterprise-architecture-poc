using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products.Events;

/// <summary>
/// 商品更新ドメインイベント
///
/// 商品の基本情報（名前、説明、価格、在庫等）が更新された際に発行されるイベント。
/// このイベントはトランザクショナルOutboxパターンを通じて、
/// 他のBounded Contextや外部システムに配信される。
///
/// 用途例:
/// - 検索インデックスの更新
/// - キャッシュの無効化
/// - 監査ログの記録
/// - リアルタイム通知（SignalR）
/// </summary>
public sealed record ProductUpdatedDomainEvent(
    ProductId ProductId,
    string ProductName
) : DomainEvent;
