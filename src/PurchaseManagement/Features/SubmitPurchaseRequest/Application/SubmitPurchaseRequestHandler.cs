using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;

namespace SubmitPurchaseRequest.Application;

public class SubmitPurchaseRequestHandler : IRequestHandler<SubmitPurchaseRequestCommand, Result<Guid>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalFlowService _approvalFlowService;
    private readonly IAppContext _appContext;
    private readonly ILogger<SubmitPurchaseRequestHandler> _logger;

    public SubmitPurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        IApprovalFlowService approvalFlowService,
        IAppContext appContext,
        ILogger<SubmitPurchaseRequestHandler> logger)
    {
        _repository = repository;
        _approvalFlowService = approvalFlowService;
        _appContext = appContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SubmitPurchaseRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 購買申請を作成
            var tenantId = _appContext.TenantId ?? throw new InvalidOperationException("TenantIdが設定されていません");

            var request = PurchaseRequest.Create(
                _appContext.UserId,
                _appContext.UserName,
                command.Title,
                command.Description,
                tenantId
            );

            // 2. 明細を追加
            foreach (var item in command.Items)
            {
                request.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
            }

            // 3. 承認フローを決定（金額に応じて自動判定）
            var approvalFlow = await _approvalFlowService.DetermineFlowAsync(
                request.TotalAmount.Amount,
                cancellationToken
            );

            // 4. 申請提出
            request.Submit(approvalFlow);

            // 5. 永続化
            await _repository.SaveAsync(request, cancellationToken);

            _logger.LogInformation(
                "Purchase request submitted: RequestId={RequestId}, RequestNumber={RequestNumber}, TotalAmount={TotalAmount}",
                request.Id, request.RequestNumber.Value, request.TotalAmount.Amount);

            return Result.Success(request.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to submit purchase request: {Message}", ex.Message);
            return Result.Fail<Guid>(ex.Message);
        }
    }
}
