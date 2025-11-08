using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using Shared.Application;
using Shared.Kernel;

namespace ApprovePurchaseRequest.Application;

public class ApprovePurchaseRequestHandler : IRequestHandler<ApprovePurchaseRequestCommand, Result<Unit>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApprovePurchaseRequestHandler> _logger;

    public ApprovePurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        ILogger<ApprovePurchaseRequestHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
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

            // 2. 承認処理
            request.Approve(_currentUserService.UserId!.Value, command.Comment);

            // 3. 永続化
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
