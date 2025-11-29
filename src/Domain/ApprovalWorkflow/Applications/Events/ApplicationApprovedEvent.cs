using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 最終承認完了イベント
/// </summary>
public sealed record ApplicationApprovedEvent(
    Guid ApplicationId,
    Guid ApplicantId,
    Guid FinalApproverId,
    DateTime ApprovedAt
) : DomainEvent;
