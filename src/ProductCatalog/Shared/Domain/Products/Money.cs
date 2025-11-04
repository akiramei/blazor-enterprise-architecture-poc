using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 金額（Value Object）
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
