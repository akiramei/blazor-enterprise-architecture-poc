using GetPurchaseRequests.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PurchaseManagement.Infrastructure.Persistence;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// EF Core版GetPurchaseRequestsハンドラ（Fast テスト用）
///
/// Dapperハンドラと同等の結果を返すが、EF Coreを使用してInMemory/SQLiteデータベースに対応
/// </summary>
public sealed class EfGetPurchaseRequestsHandler : IRequestHandler<GetPurchaseRequestsQuery, Result<List<PurchaseRequestListItemDto>>>
{
    private readonly PurchaseManagementDbContext _context;

    public EfGetPurchaseRequestsHandler(PurchaseManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PurchaseRequestListItemDto>>> Handle(
        GetPurchaseRequestsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryable = _context.PurchaseRequests.AsQueryable();

            // フィルタリング
            if (query.Status.HasValue)
            {
                queryable = queryable.Where(pr => (int)pr.Status == query.Status.Value);
            }

            if (query.RequesterId.HasValue)
            {
                queryable = queryable.Where(pr => pr.RequesterId == query.RequesterId.Value);
            }

            // ソート
            queryable = query.SortBy switch
            {
                "RequestNumber" => query.Ascending
                    ? queryable.OrderBy(pr => pr.RequestNumber.Value)
                    : queryable.OrderByDescending(pr => pr.RequestNumber.Value),
                "Title" => query.Ascending
                    ? queryable.OrderBy(pr => pr.Title)
                    : queryable.OrderByDescending(pr => pr.Title),
                "Status" => query.Ascending
                    ? queryable.OrderBy(pr => pr.Status)
                    : queryable.OrderByDescending(pr => pr.Status),
                "TotalAmount" => query.Ascending
                    ? queryable.OrderBy(pr => pr.Items.Sum(i => i.Amount.Amount))
                    : queryable.OrderByDescending(pr => pr.Items.Sum(i => i.Amount.Amount)),
                "SubmittedAt" => query.Ascending
                    ? queryable.OrderBy(pr => pr.SubmittedAt)
                    : queryable.OrderByDescending(pr => pr.SubmittedAt),
                _ => query.Ascending
                    ? queryable.OrderBy(pr => pr.CreatedAt)
                    : queryable.OrderByDescending(pr => pr.CreatedAt)
            };

            // ページング
            var items = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(pr => new
                {
                    pr.Id,
                    RequestNumber = pr.RequestNumber.Value,
                    pr.RequesterId,
                    pr.RequesterName,
                    pr.Title,
                    Status = (int)pr.Status,
                    pr.CreatedAt,
                    pr.SubmittedAt,
                    pr.ApprovedAt,
                    pr.RejectedAt,
                    TotalAmount = pr.Items.Sum(i => i.Amount.Amount),
                    Currency = pr.Items.Any() ? pr.Items.First().Amount.Currency : "JPY",
                    ApprovalStepCount = pr.ApprovalSteps.Count,
                    ApprovedStepCount = pr.ApprovalSteps.Count(step => (int)step.Status == 1)
                })
                .ToListAsync(cancellationToken);

            var result = items
                .Select(item => new PurchaseRequestListItemDto
                {
                    Id = item.Id,
                    RequestNumber = item.RequestNumber,
                    RequesterId = item.RequesterId,
                    RequesterName = item.RequesterName,
                    Title = item.Title,
                    Status = item.Status,
                    StatusName = GetStatusName(item.Status),
                    CreatedAt = item.CreatedAt,
                    SubmittedAt = item.SubmittedAt,
                    ApprovedAt = item.ApprovedAt,
                    RejectedAt = item.RejectedAt,
                    TotalAmount = item.TotalAmount,
                    Currency = item.Currency,
                    ApprovalStepCount = item.ApprovalStepCount,
                    ApprovedStepCount = item.ApprovedStepCount
                })
                .ToList();

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<PurchaseRequestListItemDto>>(
                $"購買申請一覧の取得中にエラーが発生しました: {ex.Message}");
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
}
