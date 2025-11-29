using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 申請提出イベント
/// </summary>
public sealed record ApplicationSubmittedEvent(
    Guid ApplicationId,
    Guid ApplicantId,
    ApplicationType ApplicationType,
    DateTime SubmittedAt
) : DomainEvent;
