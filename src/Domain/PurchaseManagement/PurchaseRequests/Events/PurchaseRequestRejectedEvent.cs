using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Events;

/// <summary>
/// 購買申請が却下されたイベント
/// </summary>
public sealed record PurchaseRequestRejectedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    string Reason,
    DateTime RejectedAt
) : DomainEvent;
