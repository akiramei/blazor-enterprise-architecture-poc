using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 差し戻しイベント
/// </summary>
public sealed record ApplicationReturnedEvent(
    Guid ApplicationId,
    Guid ApplicantId,
    Guid ReturnedBy,
    string Reason,
    DateTime ReturnedAt
) : DomainEvent;
