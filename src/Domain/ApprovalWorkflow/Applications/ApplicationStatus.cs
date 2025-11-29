namespace Domain.ApprovalWorkflow.Applications;

/// <summary>
/// 申請のステータス
///
/// 【パターン: State Machine Pattern】
///
/// 状態遷移:
/// - Draft → Submitted（提出）
/// - Submitted → InReview（ワークフロー開始）
/// - InReview → Approved（最終承認）/ Rejected（却下）/ Returned（差し戻し）
/// - Returned → Submitted（再提出）
/// - Approved, Rejected → 終端状態
/// </summary>
public enum ApplicationStatus
{
    /// <summary>下書き（未提出）</summary>
    Draft = 0,

    /// <summary>提出済み（ワークフロー開始待ち）</summary>
    Submitted = 1,

    /// <summary>承認待ち（現在あるステップで承認待ち）</summary>
    InReview = 2,

    /// <summary>最終承認済み</summary>
    Approved = 3,

    /// <summary>却下</summary>
    Rejected = 4,

    /// <summary>差し戻し（修正して再提出可能）</summary>
    Returned = 5
}
