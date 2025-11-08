using ProductCatalog.Shared.Domain.Products;
using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests;

/// <summary>
/// 購買申請明細
/// </summary>
public class PurchaseRequestItem : Entity
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Money UnitPrice { get; init; } = null!;
    public int Quantity { get; init; }
    public Money Amount { get; init; } = null!;

    private PurchaseRequestItem() { } // For EF Core

    public static PurchaseRequestItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("数量は1以上を指定してください");

        if (unitPrice <= 0)
            throw new DomainException("単価は0円より大きい金額を指定してください");

        return new PurchaseRequestItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            UnitPrice = new Money(unitPrice),
            Quantity = quantity,
            Amount = new Money(unitPrice * quantity)
        };
    }
}
