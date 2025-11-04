using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products.Events;

/// <summary>
/// 商品削除ドメインイベント
/// </summary>
public sealed record ProductDeletedDomainEvent(ProductId ProductId, string ProductName) : DomainEvent;
