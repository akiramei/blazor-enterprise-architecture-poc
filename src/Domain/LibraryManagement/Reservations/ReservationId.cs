using Shared.Kernel;

namespace Domain.LibraryManagement.Reservations;

/// <summary>
/// 予約ID（Value Object）
///
/// 【カタログパターン: domain-typed-id】
/// - プリミティブ型の誤用を防止
/// - コンパイル時の型安全性を保証
/// </summary>
public sealed class ReservationId : ValueObject
{
    public Guid Value { get; }

    public ReservationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("予約IDは空にできません");
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static ReservationId New() => new(Guid.NewGuid());

    public static implicit operator Guid(ReservationId reservationId) => reservationId.Value;
}
