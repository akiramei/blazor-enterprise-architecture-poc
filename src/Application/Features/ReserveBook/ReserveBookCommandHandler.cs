using Domain.LibraryManagement.Loans;
using Domain.LibraryManagement.Reservations;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;

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
public sealed class ReserveBookCommandHandler : IRequestHandler<ReserveBookCommand, Result<Guid>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<ReserveBookCommandHandler> _logger;

    // TODO: 本番では IMemberRepository, IBookCopyRepository を注入
    // private readonly IMemberRepository _memberRepository;
    // private readonly IBookCopyRepository _bookCopyRepository;

    public ReserveBookCommandHandler(
        IReservationRepository reservationRepository,
        ILogger<ReserveBookCommandHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(ReserveBookCommand request, CancellationToken ct)
    {
        var memberId = new MemberId(request.MemberId);
        var bookCopyId = new BookCopyId(request.BookCopyId);

        // DR3: 同一会員が同じ本を既に予約していないかチェック
        var existingReservation = await _reservationRepository.GetByMemberAndBookAsync(
            memberId, bookCopyId, ct);

        if (existingReservation != null)
        {
            _logger.LogWarning(
                "予約重複: MemberId={MemberId}, BookCopyId={BookCopyId}",
                request.MemberId, request.BookCopyId);
            return Result.Fail<Guid>("この本は既に予約済みです");
        }

        // DR1: 予約上限チェック
        var activeReservations = await _reservationRepository.GetActiveByMemberIdAsync(memberId, ct);
        if (activeReservations.Count >= 3)
        {
            _logger.LogWarning(
                "予約上限超過: MemberId={MemberId}, Count={Count}",
                request.MemberId, activeReservations.Count);
            return Result.Fail<Guid>("予約上限（3冊）に達しています");
        }

        // TODO: DR2: 本が貸出中かどうかチェック
        // var bookCopy = await _bookCopyRepository.GetByIdAsync(bookCopyId, ct);
        // if (bookCopy?.IsAvailable == true)
        // {
        //     return Result.Fail<Guid>("この本は現在貸出可能です。予約ではなく貸出をご利用ください");
        // }

        // 予約順位を取得
        var queuePosition = await _reservationRepository.GetQueuePositionAsync(bookCopyId, ct) + 1;

        // 予約を作成
        var reservation = Reservation.Create(memberId, bookCopyId, queuePosition);

        await _reservationRepository.AddAsync(reservation, ct);

        _logger.LogInformation(
            "予約登録: ReservationId={ReservationId}, MemberId={MemberId}, BookCopyId={BookCopyId}, Position={Position}",
            reservation.Id.Value, request.MemberId, request.BookCopyId, queuePosition);

        // SaveChangesAsync は呼ばない（TransactionBehavior が自動実行）
        return Result.Success(reservation.Id.Value);
    }
}
