using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Events;

/// <summary>
/// 購買申請が却下されたイベント
/// </summary>
public record PurchaseRequestRejectedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    string Reason,
    DateTime RejectedAt
) : DomainEvent;
