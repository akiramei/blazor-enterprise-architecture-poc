namespace Domain.ApprovalWorkflow.Boundaries;

/// <summary>
/// 申請のBoundaryインターフェース
///
/// 【パターン: Boundary Pattern】
///
/// 責務:
/// - 「誰が・何に対して・何をできるか」の判定
/// - ビジネスルールに基づく実行可否の判定
///
/// 設計:
/// - BoundaryServiceは「データ取得」と「Entityへの委譲」のみを行う
/// - 業務ロジックはEntity.CanXxx()メソッドに実装
/// </summary>
public interface IApplicationBoundary
{
    /// <summary>
    /// 編集可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateEditAsync(
        Guid applicationId,
        Guid editorId,
        CancellationToken ct = default);

    /// <summary>
    /// 提出可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateSubmitAsync(
        Guid applicationId,
        CancellationToken ct = default);

    /// <summary>
    /// 再提出可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateResubmitAsync(
        Guid applicationId,
        Guid applicantId,
        CancellationToken ct = default);

    /// <summary>
    /// 承認可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateApproveAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default);

    /// <summary>
    /// 却下可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateRejectAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default);

    /// <summary>
    /// 差し戻し可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateReturnAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default);

    /// <summary>
    /// 閲覧可否を判定
    /// </summary>
    Task<BoundaryDecision> ValidateViewAsync(
        Guid applicationId,
        Guid userId,
        string userRole,
        CancellationToken ct = default);
}
