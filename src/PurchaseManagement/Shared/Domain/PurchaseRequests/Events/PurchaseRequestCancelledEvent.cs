using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Events;

/// <summary>
/// 購買申請がキャンセルされたイベント
/// </summary>
public record PurchaseRequestCancelledEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    DateTime CancelledAt
) : DomainEvent;
