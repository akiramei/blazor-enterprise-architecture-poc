using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Products.Events;

/// <summary>
/// 商品価格変更ドメインイベント
/// </summary>
public sealed record ProductPriceChangedDomainEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice
) : DomainEvent;
