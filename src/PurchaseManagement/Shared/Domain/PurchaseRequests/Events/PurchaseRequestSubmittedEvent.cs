using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Events;

/// <summary>
/// 購買申請が提出されたイベント
/// </summary>
public record PurchaseRequestSubmittedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    string RequesterName,
    decimal TotalAmount,
    DateTime SubmittedAt
) : DomainEvent;
