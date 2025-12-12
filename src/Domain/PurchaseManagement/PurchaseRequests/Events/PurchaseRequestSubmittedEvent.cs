using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Events;

/// <summary>
/// 購買申請が提出されたイベント
/// </summary>
public sealed record PurchaseRequestSubmittedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    string RequesterName,
    decimal TotalAmount,
    DateTime SubmittedAt
) : DomainEvent;
