using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using Shared.Application;
using Shared.Kernel;

namespace RejectPurchaseRequest.Application;

public class RejectPurchaseRequestHandler : IRequestHandler<RejectPurchaseRequestCommand, Result<Unit>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RejectPurchaseRequestHandler> _logger;

    public RejectPurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService,
        ILogger<RejectPurchaseRequestHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(RejectPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 購買申請を取得
            var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);
            if (request is null)
            {
                return Result.Fail<Unit>("購買申請が見つかりません");
            }

            // 2. 却下処理
            request.Reject(_currentUserService.UserId!.Value, command.Reason);

            // 3. 永続化
            await _repository.SaveAsync(request, cancellationToken);

            _logger.LogInformation(
                "Purchase request rejected: RequestId={RequestId}, RequestNumber={RequestNumber}, RejecterId={RejecterId}, Reason={Reason}",
                request.Id, request.RequestNumber.Value, _currentUserService.UserId, command.Reason);

            return Result.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to reject purchase request: {Message}", ex.Message);
            return Result.Fail<Unit>(ex.Message);
        }
    }
}
