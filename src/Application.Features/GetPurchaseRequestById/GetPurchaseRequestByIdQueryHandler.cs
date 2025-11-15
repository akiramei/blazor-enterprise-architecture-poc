using Application.Core.Queries;
using Domain.PurchaseManagement.PurchaseRequests;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.GetPurchaseRequestById;

/// <summary>
/// 購買申請詳細取得クエリハンドラー (工業製品化版)
/// </summary>
public class GetPurchaseRequestByIdQueryHandler
    : QueryPipeline<GetPurchaseRequestByIdQuery, PurchaseRequestDetailDto>
{
    private readonly IPurchaseRequestRepository _repository;

    public GetPurchaseRequestByIdQueryHandler(IPurchaseRequestRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<PurchaseRequestDetailDto>> ExecuteAsync(
        GetPurchaseRequestByIdQuery query,
        CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(query.Id, ct);
        if (request is null)
            return Result.Fail<PurchaseRequestDetailDto>("購買申請が見つかりません");

        var dto = new PurchaseRequestDetailDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber.Value,
            Title = request.Title,
            Description = request.Description,
            RequesterId = request.RequesterId,
            RequesterName = request.RequesterName,
            Status = (int)request.Status,
            StatusName = GetStatusName(request.Status),
            TotalAmount = request.TotalAmount.Amount,
            Currency = request.TotalAmount.Currency,
            CreatedAt = request.CreatedAt,
            SubmittedAt = request.SubmittedAt,
            ApprovedAt = request.ApprovedAt,
            RejectedAt = request.RejectedAt,
            Items = request.Items.Select(i => new PurchaseRequestItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice.Amount,
                Quantity = i.Quantity,
                Amount = i.Amount.Amount
            }).ToList(),
            ApprovalSteps = request.ApprovalSteps.Select(s => new ApprovalStepDto
            {
                StepNumber = s.StepNumber,
                ApproverName = s.ApproverName,
                ApproverRole = s.ApproverRole,
                Status = (int)s.Status,
                Comment = s.Comment,
                ApprovedAt = s.ApprovedAt
            }).ToList()
        };

        return Result.Success(dto);
    }

    private static string GetStatusName(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.Draft => "下書き",
            PurchaseRequestStatus.Submitted => "提出済み",
            PurchaseRequestStatus.PendingFirstApproval => "1次承認待ち",
            PurchaseRequestStatus.PendingSecondApproval => "2次承認待ち",
            PurchaseRequestStatus.PendingFinalApproval => "最終承認待ち",
            PurchaseRequestStatus.Approved => "承認済み",
            PurchaseRequestStatus.Rejected => "却下",
            PurchaseRequestStatus.Cancelled => "キャンセル",
            _ => "不明"
        };
    }
}
