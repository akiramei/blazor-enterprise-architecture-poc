using Shared.Application;
using Shared.Application.Interfaces;

namespace GetPurchaseRequestById.Application;

/// <summary>
/// 購買申請詳細取得クエリ
///
/// 【パターン: CQRS Query】
///
/// 責務:
/// - 指定されたIDの購買申請詳細を取得
/// - 承認ステップと品目を含む完全なデータ取得
///
/// AI実装時の注意:
/// - IQuery<TResponse>を使用
/// - Dapperによる複数結果セットの取得で効率化
/// </summary>
public sealed record GetPurchaseRequestByIdQuery : IQuery<Result<PurchaseRequestDetailDto?>>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// 購買申請詳細DTO
/// </summary>
public sealed record PurchaseRequestDetailDto
{
    public required Guid Id { get; init; }
    public required string RequestNumber { get; init; }
    public required Guid RequesterId { get; init; }
    public required string RequesterName { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public required List<ApprovalStepDto> ApprovalSteps { get; init; }
    public required List<PurchaseRequestItemDetailDto> Items { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
}

/// <summary>
/// 承認ステップDTO
/// </summary>
public sealed record ApprovalStepDto
{
    public required Guid Id { get; init; }
    public required int StepNumber { get; init; }
    public required Guid ApproverId { get; init; }
    public required string ApproverName { get; init; }
    public required string ApproverRole { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public string? Comment { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
}

/// <summary>
/// 購買申請品目詳細DTO
/// </summary>
public sealed record PurchaseRequestItemDetailDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal UnitPrice { get; init; }
    public required string Currency { get; init; }
    public required int Quantity { get; init; }
    public required decimal Amount { get; init; }
}
