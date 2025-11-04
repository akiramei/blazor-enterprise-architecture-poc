using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products.Events;

/// <summary>
/// 商品価格変更ドメインイベント
/// </summary>
public sealed record ProductPriceChangedDomainEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice
) : DomainEvent;
