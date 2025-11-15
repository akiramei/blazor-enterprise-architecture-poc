using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Events;

/// <summary>
/// 購買申請がキャンセルされたイベント
/// </summary>
public record PurchaseRequestCancelledEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    DateTime CancelledAt
) : DomainEvent;
