using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 申請再提出イベント
/// </summary>
public sealed record ApplicationResubmittedEvent(
    Guid ApplicationId,
    Guid ApplicantId,
    DateTime ResubmittedAt
) : DomainEvent;
