using Shared.Kernel;

namespace Domain.ProductCatalog.Products.Events;

/// <summary>
/// 商品削除ドメインイベント
/// </summary>
public sealed record ProductDeletedDomainEvent(ProductId ProductId, string ProductName) : DomainEvent;
