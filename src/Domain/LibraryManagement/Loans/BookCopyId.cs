using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 蔵書ID（Value Object）
/// </summary>
public sealed class BookCopyId : ValueObject
{
    public Guid Value { get; }

    public BookCopyId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("蔵書IDは空にできません");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static BookCopyId New() => new(Guid.NewGuid());

    public static implicit operator Guid(BookCopyId bookCopyId) => bookCopyId.Value;
}
