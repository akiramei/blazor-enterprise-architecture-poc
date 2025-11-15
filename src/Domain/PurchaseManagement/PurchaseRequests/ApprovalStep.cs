using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests;

/// <summary>
/// 承認ステップ（エンティティ）
/// </summary>
public class ApprovalStep : Entity
{
    public Guid Id { get; init; }
    public int StepNumber { get; init; }
    public Guid ApproverId { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public string ApproverRole { get; init; } = string.Empty;
    public ApprovalStepStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

    public bool IsPending => Status == ApprovalStepStatus.Pending;
    public bool IsApproved => Status == ApprovalStepStatus.Approved;
    public bool IsRejected => Status == ApprovalStepStatus.Rejected;

    private ApprovalStep() { } // For EF Core

    public ApprovalStep(int stepNumber, Guid approverId, string approverName, string approverRole)
    {
        Id = Guid.NewGuid();
        StepNumber = stepNumber;
        ApproverId = approverId;
        ApproverName = approverName;
        ApproverRole = approverRole;
        Status = ApprovalStepStatus.Pending;
    }

    /// <summary>
    /// 承認
    /// </summary>
    public void Approve(string comment)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        Status = ApprovalStepStatus.Approved;
        Comment = comment;
        ApprovedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 却下
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("却下理由は必須です");

        Status = ApprovalStepStatus.Rejected;
        Comment = reason;
        RejectedAt = DateTime.UtcNow;
    }
}
