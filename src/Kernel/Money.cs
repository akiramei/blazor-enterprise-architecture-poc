namespace Shared.Kernel;

/// <summary>
/// 金額（Value Object）
///
/// 【Shared.Kernel配置理由】
/// - Money は複数の Bounded Context で使用される共通の値オブジェクト
/// - ProductCatalog BC と PurchaseManagement BC の両方で必要
/// - BC間の依存を避けるため、Shared.Kernel に配置
///
/// 【VSA原則との整合性】
/// - Vertical Slice Architecture では、BC間の直接依存は禁止
/// - 共通の値オブジェクトは Shared.Kernel に配置し、両BCから参照
/// - これにより、BC間の独立性を保ちつつ、コード重複を避ける
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "JPY")
    {
        if (amount < 0)
        {
            throw new DomainException("金額は0以上である必要があります");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("通貨コードは必須です");
        }

        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <summary>
    /// 表示用文字列（¥1,000形式）
    /// </summary>
    public string ToDisplayString()
    {
        return Currency switch
        {
            "JPY" => $"¥{Amount:N0}",
            "USD" => $"${Amount:N2}",
            "EUR" => $"€{Amount:N2}",
            _ => $"{Amount:N2} {Currency}"
        };
    }

    public override string ToString() => ToDisplayString();

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException("異なる通貨を加算できません");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException("異なる通貨を減算できません");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
}
