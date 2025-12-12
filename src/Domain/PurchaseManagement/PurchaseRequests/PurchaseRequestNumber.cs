using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests;

/// <summary>
/// 購買申請番号（ValueObject）
/// </summary>
public sealed class PurchaseRequestNumber : ValueObject
{
    public string Value { get; }

    public PurchaseRequestNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("購買申請番号は必須です");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// 購買申請番号を生成（PR-YYYYMMDD-XXXXXXXX形式）
    /// </summary>
    public static PurchaseRequestNumber Generate()
    {
        var number = $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        return new PurchaseRequestNumber(number);
    }

    public override string ToString() => Value;
}
