using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 会員ID（Value Object）
/// </summary>
public sealed class MemberId : ValueObject
{
    public Guid Value { get; }

    public MemberId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("会員IDは空にできません");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static MemberId New() => new(Guid.NewGuid());

    public static implicit operator Guid(MemberId memberId) => memberId.Value;
}
