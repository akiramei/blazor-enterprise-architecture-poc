using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Events;

/// <summary>
/// 購買申請が承認されたイベント
/// </summary>
public record PurchaseRequestApprovedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    decimal TotalAmount,
    DateTime ApprovedAt
) : DomainEvent;
