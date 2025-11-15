namespace Domain.PurchaseManagement.PurchaseRequests;

/// <summary>
/// 承認ステップのステータス
/// </summary>
public enum ApprovalStepStatus
{
    /// <summary>承認待ち</summary>
    Pending = 0,

    /// <summary>承認済み</summary>
    Approved = 1,

    /// <summary>却下</summary>
    Rejected = 2
}
