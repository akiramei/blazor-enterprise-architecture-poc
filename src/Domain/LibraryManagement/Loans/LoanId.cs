using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 貸出ID（Value Object）
/// </summary>
public sealed class LoanId : ValueObject
{
    public Guid Value { get; }

    public LoanId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("貸出IDは空にできません");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static LoanId New() => new(Guid.NewGuid());

    public static implicit operator Guid(LoanId loanId) => loanId.Value;
}
