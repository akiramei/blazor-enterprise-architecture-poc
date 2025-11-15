using Application.Core.Commands;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
using MediatR;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.RejectPurchaseRequest;

/// <summary>
/// 購買申請却下コマンドハンドラー (工業製品化版)
///
/// 【リファクタリング成果】
/// - Before: 70行
/// - After: 14行
/// - 削減率: 80%
/// </summary>
public class RejectPurchaseRequestCommandHandler
    : CommandPipeline<RejectPurchaseRequestCommand, Unit>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApprovalBoundary _approvalBoundary;

    public RejectPurchaseRequestCommandHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        IApprovalBoundary approvalBoundary)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _approvalBoundary = approvalBoundary;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        RejectPurchaseRequestCommand cmd,
        CancellationToken ct)
    {
        // 1. 購買申請を取得
        var request = await _repository.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result.Fail<Unit>("購買申請が見つかりません");

        // 2. 却下資格チェック (Boundary経由)
        var eligibility = _approvalBoundary.CheckEligibility(request, _currentUserService.UserId);
        if (!eligibility.CanReject)
        {
            var reasons = string.Join(", ", eligibility.BlockingReasons.Select(r => r.Message));
            return Result.Fail<Unit>(reasons);
        }

        // 3. 却下処理 (ドメインロジック)
        request.Reject(_currentUserService.UserId, cmd.Reason);

        // 4. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(Unit.Value);
    }
}
