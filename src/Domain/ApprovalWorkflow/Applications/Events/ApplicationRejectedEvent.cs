using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications.Events;

/// <summary>
/// 却下イベント
/// </summary>
public sealed record ApplicationRejectedEvent(
    Guid ApplicationId,
    Guid ApplicantId,
    Guid RejectedBy,
    string Reason,
    DateTime RejectedAt
) : DomainEvent;
