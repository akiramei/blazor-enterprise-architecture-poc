namespace Domain.ApprovalWorkflow.Applications.Boundaries;

/// <summary>
/// 申請に対する操作の意図
///
/// 【パターン: Boundary Pattern - Intent】
///
/// 責務:
/// - 操作の種類を列挙
/// - Boundaryが判定対象とするアクションを定義
/// </summary>
public enum ApplicationIntent
{
    /// <summary>作成（下書き）</summary>
    Create,

    /// <summary>編集（下書きのみ）</summary>
    Edit,

    /// <summary>提出</summary>
    Submit,

    /// <summary>再提出（差し戻し後）</summary>
    Resubmit,

    /// <summary>閲覧</summary>
    View,

    /// <summary>承認</summary>
    Approve,

    /// <summary>却下</summary>
    Reject,

    /// <summary>差し戻し</summary>
    Return
}
