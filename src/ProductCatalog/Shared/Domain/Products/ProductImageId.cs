using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 商品画像ID（Value Object）
/// </summary>
public sealed class ProductImageId : ValueObject
{
    public Guid Value { get; }

    private ProductImageId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("商品画像IDは空にできません", nameof(value));
        }

        Value = value;
    }

    public static ProductImageId New() => new(Guid.NewGuid());

    public static ProductImageId From(Guid value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ProductImageId id) => id.Value;
}
