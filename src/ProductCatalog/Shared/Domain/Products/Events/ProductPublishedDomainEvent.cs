using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products.Events;

/// <summary>
/// 商品公開ドメインイベント
/// </summary>
public sealed record ProductPublishedDomainEvent(ProductId ProductId) : DomainEvent;
