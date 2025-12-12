using Domain.LibraryManagement.Reservations;
using Application.Core.Commands;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Domain.LibraryManagement.Loans;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録コマンドハンドラー
///
/// 【SPEC準拠】
/// - DR1: 1会員あたり同時予約上限 = 3冊
/// - DR2: 予約は貸出中の本のみ可能
/// - DR3: 同一会員が同じ本を複数回予約することは不可
///
/// 【カタログパターン: feature-create-entity】
/// - SaveChangesAsync は呼ばない（TransactionBehavior に委譲）
/// - Result<T> パターンでエラーを返す
/// </summary>
public sealed class ReserveBookCommandHandler
    : CommandPipeline<ReserveBookCommand, Guid>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ILogger<ReserveBookCommandHandler> _logger;

    public ReserveBookCommandHandler(
        IReservationRepository reservationRepository,
        ILoanRepository loanRepository,
        ILogger<ReserveBookCommandHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _loanRepository = loanRepository;
        _logger = logger;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        ReserveBookCommand request,
        CancellationToken ct)
    {
        var member = await _loanRepository.GetMemberByBarcodeAsync(request.MemberBarcode, ct);
        if (member is null)
            return Result.Fail<Guid>("会員が見つかりません");
        if (!member.IsActive)
            return Result.Fail<Guid>("無効な会員のため予約できません");

        var bookCopy = await _loanRepository.GetBookCopyByBarcodeAsync(request.BookCopyBarcode, ct);
        if (bookCopy is null)
            return Result.Fail<Guid>("蔵書が見つかりません");

        // DR2: 予約は貸出中の本のみ可能（貸出可能なら予約不可）
        if (bookCopy.IsReferenceOnly)
            return Result.Fail<Guid>("参考図書は予約できません");
        if (bookCopy.IsAvailableForLoan)
            return Result.Fail<Guid>("この本は現在貸出可能です。予約ではなく貸出をご利用ください");

        var memberId = member.Id;
        var bookCopyId = bookCopy.Id;

        // DR3: 同一会員が同じ本を既に予約していないかチェック
        var existingReservation = await _reservationRepository.GetByMemberAndBookAsync(
            memberId, bookCopyId, ct);

        if (existingReservation != null)
        {
            _logger.LogWarning(
                "予約重複: MemberId={MemberId}, BookCopyId={BookCopyId}",
                memberId, bookCopyId);
            return Result.Fail<Guid>("この本は既に予約済みです");
        }

        // DR1: 予約上限チェック
        var activeReservations = await _reservationRepository.GetActiveByMemberIdAsync(memberId, ct);
        if (activeReservations.Count >= 3)
        {
            _logger.LogWarning(
                "予約上限超過: MemberId={MemberId}, Count={Count}",
                memberId, activeReservations.Count);
            return Result.Fail<Guid>("予約上限（3冊）に達しています");
        }

        // 予約順位を取得
        var queuePosition = await _reservationRepository.GetQueuePositionAsync(bookCopyId, ct) + 1;

        // 予約を作成
        var reservation = Reservation.Create(memberId, bookCopyId, queuePosition);

        await _reservationRepository.AddAsync(reservation, ct);

        _logger.LogInformation(
            "予約登録: ReservationId={ReservationId}, MemberId={MemberId}, BookCopyId={BookCopyId}, Position={Position}",
            reservation.Id.Value, memberId, bookCopyId, queuePosition);

        // SaveChangesAsync は呼ばない（TransactionBehavior が自動実行）
        return Result.Success(reservation.Id.Value);
    }
}
