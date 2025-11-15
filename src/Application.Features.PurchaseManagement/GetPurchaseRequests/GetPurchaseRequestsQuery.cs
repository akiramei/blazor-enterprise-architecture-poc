using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.PurchaseManagement.GetPurchaseRequests;

/// <summary>
/// 購買申請一覧取得クエリ
/// </summary>
public class GetPurchaseRequestsQuery : IQuery<Result<List<PurchaseRequestListItemDto>>>
{
    public int? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "CreatedAt";
    public bool Ascending { get; init; }
}

/// <summary>
/// 購買申請一覧項目DTO
/// </summary>
public class PurchaseRequestListItemDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "JPY";
    public int ApprovalStepCount { get; init; }
    public int ApprovedStepCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
