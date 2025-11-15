using Shared.Kernel;

namespace Domain.ProductCatalog.Products.Events;

/// <summary>
/// 商品価格変更ドメインイベント
/// </summary>
public sealed record ProductPriceChangedDomainEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice
) : DomainEvent;
