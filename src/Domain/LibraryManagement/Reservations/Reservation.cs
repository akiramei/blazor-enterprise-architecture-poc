using Domain.LibraryManagement.Boundaries;
using Domain.LibraryManagement.Loans;
using Shared.Kernel;

namespace Domain.LibraryManagement.Reservations;

/// <summary>
/// 予約エンティティ（AggregateRoot）
///
/// 【SPEC準拠】
/// - DR1: 1会員あたり同時予約上限 = 3冊
/// - DR2: 予約は貸出中の本のみ可能
/// - DR3: 同一会員が同じ本を複数回予約することは不可
/// - DR4: 予約順位は予約日時の早い順
///
/// 【カタログパターン: entity-base】
/// - Entityを継承
/// - CanXxx()メソッドで操作可否を判定
/// </summary>
public sealed class Reservation : AggregateRoot<ReservationId>
{
    public MemberId MemberId { get; private set; }
    public BookCopyId BookCopyId { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public int QueuePosition { get; private set; }
    public ReservationStatus Status { get; private set; }

    private Reservation() : base(default!)
    {
        MemberId = default!;
        BookCopyId = default!;
    }

    private Reservation(
        ReservationId id,
        MemberId memberId,
        BookCopyId bookCopyId,
        int queuePosition)
        : base(id)
    {
        MemberId = memberId;
        BookCopyId = bookCopyId;
        ReservedAt = DateTime.UtcNow;
        QueuePosition = queuePosition;
        Status = ReservationStatus.Active;
    }

    /// <summary>
    /// 予約を作成
    /// </summary>
    public static Reservation Create(
        MemberId memberId,
        BookCopyId bookCopyId,
        int queuePosition)
    {
        return new Reservation(
            ReservationId.New(),
            memberId,
            bookCopyId,
            queuePosition);
    }

    /// <summary>
    /// 予約をキャンセル可能か判定
    /// </summary>
    public BoundaryDecision CanCancel()
    {
        if (Status == ReservationStatus.Cancelled)
            return BoundaryDecision.Deny("既にキャンセル済みです");

        if (Status == ReservationStatus.Fulfilled)
            return BoundaryDecision.Deny("既に貸出済みです");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 予約をキャンセル
    /// </summary>
    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
    }

    /// <summary>
    /// 予約を履行（貸出に変換）
    /// </summary>
    public void Fulfill()
    {
        Status = ReservationStatus.Fulfilled;
    }
}

/// <summary>
/// 予約ステータス
/// </summary>
public enum ReservationStatus
{
    /// <summary>有効（待機中）</summary>
    Active,

    /// <summary>キャンセル済み</summary>
    Cancelled,

    /// <summary>履行済み（貸出に変換）</summary>
    Fulfilled
}
