using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetPendingApprovals;

/// <summary>
/// 承認待ち申請一覧取得クエリ
/// </summary>
public class GetPendingApprovalsQuery : IQuery<Result<List<PendingApprovalDto>>>
{
    /// <summary>
    /// ページ番号（デフォルト: 1）
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// ページサイズ（デフォルト: 20）
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// ソート順（デフォルト: 金額降順）
    /// </summary>
    public string SortBy { get; init; } = "TotalAmount";

    /// <summary>
    /// 昇順ソート（デフォルト: false = 降順）
    /// </summary>
    public bool Ascending { get; init; } = false;
}

/// <summary>
/// 承認待ち申請DTO
/// </summary>
public class PendingApprovalDto
{
    public Guid PurchaseRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public int CurrentStepNumber { get; init; }
    public int TotalSteps { get; init; }
    public int DaysSinceSubmitted => (DateTime.UtcNow - SubmittedAt).Days;
}
