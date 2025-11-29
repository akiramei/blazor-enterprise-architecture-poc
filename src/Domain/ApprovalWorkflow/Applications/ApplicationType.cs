namespace Domain.ApprovalWorkflow.Applications;

/// <summary>
/// 申請タイプ
///
/// 各申請タイプに対応するワークフロー定義（WorkflowDefinition）が存在する
/// </summary>
public enum ApplicationType
{
    /// <summary>休暇申請</summary>
    LeaveRequest = 0,

    /// <summary>経費申請</summary>
    ExpenseRequest = 1,

    /// <summary>購買依頼</summary>
    PurchaseRequest = 2
}
