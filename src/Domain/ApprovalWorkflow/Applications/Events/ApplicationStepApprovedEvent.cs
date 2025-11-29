using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 承認ステップ完了イベント（最終承認以外）
/// </summary>
public sealed record ApplicationStepApprovedEvent(
    Guid ApplicationId,
    Guid ApproverId,
    int CompletedStepNumber,
    DateTime ApprovedAt
) : DomainEvent;
