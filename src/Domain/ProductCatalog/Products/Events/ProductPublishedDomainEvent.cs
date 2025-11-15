using Shared.Kernel;

namespace Domain.ProductCatalog.Products.Events;

/// <summary>
/// 商品公開ドメインイベント
/// </summary>
public sealed record ProductPublishedDomainEvent(ProductId ProductId) : DomainEvent;
