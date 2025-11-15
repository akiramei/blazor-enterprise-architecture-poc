using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.PurchaseManagement.GetPurchaseRequestById;

/// <summary>
/// 購買申請詳細取得クエリ
/// </summary>
public class GetPurchaseRequestByIdQuery : IQuery<Result<PurchaseRequestDetailDto>>
{
    public Guid Id { get; init; }
}

/// <summary>
/// 購買申請詳細DTO
/// </summary>
public class PurchaseRequestDetailDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "JPY";
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public List<PurchaseRequestItemDto> Items { get; init; } = new();
    public List<ApprovalStepDto> ApprovalSteps { get; init; } = new();
}

public class PurchaseRequestItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
}

public class ApprovalStepDto
{
    public int StepNumber { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public string ApproverRole { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? Comment { get; init; }
    public DateTime? ApprovedAt { get; init; }
}
