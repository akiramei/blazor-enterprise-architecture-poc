using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Events;

/// <summary>
/// 購買申請が承認されたイベント
/// </summary>
public sealed record PurchaseRequestApprovedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    decimal TotalAmount,
    DateTime ApprovedAt
) : DomainEvent;
