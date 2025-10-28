using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Products.Events;

/// <summary>
/// 商品公開ドメインイベント
/// </summary>
public sealed record ProductPublishedDomainEvent(ProductId ProductId) : DomainEvent;
