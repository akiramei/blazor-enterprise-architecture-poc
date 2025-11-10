using GetPurchaseRequestById.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PurchaseManagement.Infrastructure.Persistence;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// EF Core版GetPurchaseRequestByIdハンドラ（Fast テスト用）
///
/// Dapperハンドラと同等の結果を返すが、EF Coreを使用してInMemory/SQLiteデータベースに対応
/// </summary>
public sealed class EfGetPurchaseRequestByIdHandler : IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDetailDto?>>
{
    private readonly PurchaseManagementDbContext _context;

    public EfGetPurchaseRequestByIdHandler(PurchaseManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PurchaseRequestDetailDto?>> Handle(
        GetPurchaseRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchaseRequest = await _context.PurchaseRequests
                .Where(pr => pr.Id == query.Id)
                .Select(pr => new
                {
                    pr.Id,
                    RequestNumber = pr.RequestNumber.Value,
                    pr.RequesterId,
                    pr.RequesterName,
                    pr.Title,
                    pr.Description,
                    Status = (int)pr.Status,
                    pr.CreatedAt,
                    pr.SubmittedAt,
                    pr.ApprovedAt,
                    pr.RejectedAt,
                    pr.CancelledAt,
                    ApprovalSteps = pr.ApprovalSteps.Select(step => new
                    {
                        step.Id,
                        step.StepNumber,
                        step.ApproverId,
                        step.ApproverName,
                        step.ApproverRole,
                        Status = (int)step.Status,
                        step.Comment,
                        step.ApprovedAt,
                        step.RejectedAt
                    }).OrderBy(s => s.StepNumber).ToList(),
                    Items = pr.Items.Select(item => new
                    {
                        item.Id,
                        item.ProductId,
                        item.ProductName,
                        UnitPrice = item.UnitPrice.Amount,
                        Currency = item.UnitPrice.Currency,
                        item.Quantity,
                        Amount = item.Amount.Amount
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest == null)
            {
                return Result.Success<PurchaseRequestDetailDto?>(null);
            }

            var totalAmount = purchaseRequest.Items.Sum(i => i.Amount);
            var currency = purchaseRequest.Items.FirstOrDefault()?.Currency ?? "JPY";

            var result = new PurchaseRequestDetailDto
            {
                Id = purchaseRequest.Id,
                RequestNumber = purchaseRequest.RequestNumber,
                RequesterId = purchaseRequest.RequesterId,
                RequesterName = purchaseRequest.RequesterName,
                Title = purchaseRequest.Title,
                Description = purchaseRequest.Description,
                Status = purchaseRequest.Status,
                StatusName = GetStatusName(purchaseRequest.Status),
                CreatedAt = purchaseRequest.CreatedAt,
                SubmittedAt = purchaseRequest.SubmittedAt,
                ApprovedAt = purchaseRequest.ApprovedAt,
                RejectedAt = purchaseRequest.RejectedAt,
                CancelledAt = purchaseRequest.CancelledAt,
                ApprovalSteps = purchaseRequest.ApprovalSteps.Select(s => new ApprovalStepDto
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    ApproverId = s.ApproverId,
                    ApproverName = s.ApproverName,
                    ApproverRole = s.ApproverRole,
                    Status = s.Status,
                    StatusName = GetApprovalStepStatusName(s.Status),
                    Comment = s.Comment,
                    ApprovedAt = s.ApprovedAt,
                    RejectedAt = s.RejectedAt
                }).ToList(),
                Items = purchaseRequest.Items.Select(i => new PurchaseRequestItemDetailDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Currency = i.Currency,
                    Quantity = i.Quantity,
                    Amount = i.Amount
                }).ToList(),
                TotalAmount = totalAmount,
                Currency = currency
            };

            return Result.Success<PurchaseRequestDetailDto?>(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<PurchaseRequestDetailDto?>(
                $"購買申請詳細の取得中にエラーが発生しました: {ex.Message}");
        }
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            0 => "下書き",
            1 => "提出済み",
            2 => "1次承認待ち",
            3 => "2次承認待ち",
            4 => "3次承認待ち",
            5 => "承認済み",
            6 => "却下",
            7 => "キャンセル",
            _ => "不明"
        };
    }

    private static string GetApprovalStepStatusName(int status)
    {
        return status switch
        {
            0 => "保留中",
            1 => "承認済み",
            2 => "却下",
            _ => "不明"
        };
    }
}
