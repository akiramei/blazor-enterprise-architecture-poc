using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;
using Shared.Application;
using Shared.Kernel;

namespace ApprovePurchaseRequest.Application;

public class ApprovePurchaseRequestHandler : IRequestHandler<ApprovePurchaseRequestCommand, Result<Unit>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApprovalBoundary _approvalBoundary;
    private readonly ILogger<ApprovePurchaseRequestHandler> _logger;

    public ApprovePurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        IApprovalBoundary approvalBoundary,
        ILogger<ApprovePurchaseRequestHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _approvalBoundary = approvalBoundary;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(ApprovePurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 購買申請を取得
            var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);
            if (request is null)
            {
                return Result.Fail<Unit>("購買申請が見つかりません");
            }

            // 2. 承認資格チェック（バウンダリー経由）
            var eligibility = _approvalBoundary.CheckEligibility(request, _currentUserService.UserId);
            if (!eligibility.CanApprove)
            {
                var reasons = string.Join(", ", eligibility.BlockingReasons.Select(r => r.Message));
                _logger.LogWarning(
                    "Approval not allowed: RequestId={RequestId}, UserId={UserId}, Reasons={Reasons}",
                    command.RequestId, _currentUserService.UserId, reasons);
                return Result.Fail<Unit>(reasons);
            }

            // 3. 承認処理（ドメインロジック）
            request.Approve(_currentUserService.UserId, command.Comment);

            // 4. 永続化
            await _repository.SaveAsync(request, cancellationToken);

            _logger.LogInformation(
                "Purchase request approved: RequestId={RequestId}, RequestNumber={RequestNumber}, ApproverId={ApproverId}",
                request.Id, request.RequestNumber.Value, _currentUserService.UserId);

            return Result.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to approve purchase request: {Message}", ex.Message);
            return Result.Fail<Unit>(ex.Message);
        }
    }
}
