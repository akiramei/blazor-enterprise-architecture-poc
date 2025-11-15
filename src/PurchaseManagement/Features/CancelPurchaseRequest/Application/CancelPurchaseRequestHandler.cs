using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using Domain.PurchaseManagement.PurchaseRequests;
using Shared.Application;
using Shared.Kernel;

namespace CancelPurchaseRequest.Application;

/// <summary>
/// 購買申請キャンセルハンドラー
///
/// 【パターン: CQRS Command Handler】
///
/// 責務:
/// - Repository経由でPurchaseRequestを取得
/// - 現在のユーザーIDを取得してCancel()を実行
/// - ドメインイベント (PurchaseRequestCancelledEvent) が自動発行される
/// - TransactionBehaviorによりOutboxに保存される
///
/// トランザクション:
/// - TransactionBehavior により自動管理
/// - PurchaseRequest更新 + OutboxMessage書き込みが同一トランザクション
///
/// エラーハンドリング:
/// - DomainException: ビジネスルール違反（申請者以外、承認済み等）
/// - NotFoundException: 購買申請が見つからない
/// </summary>
public sealed class CancelPurchaseRequestHandler : IRequestHandler<CancelPurchaseRequestCommand, Result>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelPurchaseRequestHandler> _logger;

    public CancelPurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        ILogger<CancelPurchaseRequestHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelPurchaseRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 購買申請を取得
            var purchaseRequest = await _repository.GetByIdAsync(request.PurchaseRequestId, cancellationToken);
            if (purchaseRequest == null)
            {
                return Result.Fail($"購買申請が見つかりません。[Id: {request.PurchaseRequestId}]");
            }

            // 現在のユーザーIDを取得
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Fail("ユーザー情報が取得できません");
            }

            // キャンセル実行（ドメインロジック）
            purchaseRequest.Cancel(userId);

            // 保存（TransactionBehaviorによりOutboxにも書き込まれる）
            await _repository.SaveAsync(purchaseRequest, cancellationToken);

            _logger.LogInformation(
                "購買申請をキャンセルしました。[Id: {PurchaseRequestId}] [User: {UserId}] [Reason: {Reason}]",
                request.PurchaseRequestId,
                userId,
                request.Reason ?? "（未指定）");

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex,
                "購買申請のキャンセルに失敗しました（ビジネスルール違反）。[Id: {PurchaseRequestId}]",
                request.PurchaseRequestId);

            return Result.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "購買申請のキャンセル中にエラーが発生しました。[Id: {PurchaseRequestId}]",
                request.PurchaseRequestId);

            return Result.Fail($"購買申請のキャンセル中にエラーが発生しました: {ex.Message}");
        }
    }
}
