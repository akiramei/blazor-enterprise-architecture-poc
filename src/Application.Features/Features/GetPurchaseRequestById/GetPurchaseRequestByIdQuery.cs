using Domain.PurchaseManagement.PurchaseRequests;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetPurchaseRequestById;

/// <summary>
/// 購買申請詳細取得クエリ
///
/// 【パターン: Entity直接返却（Boundary活用のため）】
/// UI層でバウンダリーを活用するため、DTOではなくエンティティを返す
/// </summary>
public sealed record GetPurchaseRequestByIdQuery(Guid Id) : IQuery<Result<PurchaseRequest>>;

/// <summary>
/// 購買申請詳細DTO
/// </summary>
public sealed class PurchaseRequestDetailDto
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

public sealed class PurchaseRequestItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "JPY";
}

public sealed class ApprovalStepDto
{
    public int StepNumber { get; init; }
    public Guid ApproverId { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public string ApproverRole { get; init; } = string.Empty;
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
}
