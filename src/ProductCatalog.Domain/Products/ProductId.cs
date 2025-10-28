using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Products;

/// <summary>
/// 商品ID（Value Object）
/// </summary>
public sealed class ProductId : ValueObject
{
    public Guid Value { get; }

    public ProductId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("商品IDは空にできません");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static ProductId New() => new(Guid.NewGuid());

    public static implicit operator Guid(ProductId productId) => productId.Value;
}
