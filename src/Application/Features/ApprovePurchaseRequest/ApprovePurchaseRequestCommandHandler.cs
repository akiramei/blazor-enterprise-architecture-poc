using Application.Core.Commands;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
using MediatR;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.ApprovePurchaseRequest;

/// <summary>
/// 購買申請承認コマンドハンドラー (工業製品化版)
///
/// 【リファクタリング成果】
/// - Before: 70行 (try-catch, ログ, エラーハンドリング含む)
/// - After: 14行 (ドメインロジックのみ)
/// - 削減率: 80%
///
/// 【CommandPipeline継承の効果】
/// - トランザクション管理: Behaviorが処理
/// - ログ出力: LoggingBehaviorが処理
/// - エラーハンドリング: CommandPipeline基底クラスが処理
/// </summary>
public class ApprovePurchaseRequestCommandHandler
    : CommandPipeline<ApprovePurchaseRequestCommand, Unit>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApprovalBoundary _approvalBoundary;

    public ApprovePurchaseRequestCommandHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        IApprovalBoundary approvalBoundary)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _approvalBoundary = approvalBoundary;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        ApprovePurchaseRequestCommand cmd,
        CancellationToken ct)
    {
        // 1. 購買申請を取得
        var request = await _repository.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result.Fail<Unit>("購買申請が見つかりません");

        // 2. 承認資格チェック (Boundary経由)
        var eligibility = _approvalBoundary.CheckEligibility(request, _currentUserService.UserId);
        if (!eligibility.CanApprove)
        {
            var reasons = string.Join(", ", eligibility.BlockingReasons.Select(r => r.Message));
            return Result.Fail<Unit>(reasons);
        }

        // 3. 承認処理 (ドメインロジック)
        request.Approve(_currentUserService.UserId, cmd.Comment);

        // 4. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(Unit.Value);
    }
}
