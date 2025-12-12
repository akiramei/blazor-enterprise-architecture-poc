using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Boundaries;
using Domain.ApprovalWorkflow.WorkflowDefinitions;

namespace ApprovalWorkflow.Infrastructure.Services;

/// <summary>
/// 申請のBoundaryサービス実装
///
/// 【パターン: Boundary Pattern】
///
/// 責務:
/// - 「誰が・何に対して・何をできるか」の判定
/// - データ取得とEntity.CanXxx()への委譲のみ
///
/// 設計:
/// - 業務ロジックはEntity.CanXxx()メソッドに実装済み
/// - このサービスはデータ取得と委譲のみ
/// </summary>
public sealed class ApplicationBoundaryService : IApplicationBoundary
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;

    public ApplicationBoundaryService(
        IApplicationRepository applicationRepository,
        IWorkflowDefinitionRepository workflowDefinitionRepository)
    {
        _applicationRepository = applicationRepository;
        _workflowDefinitionRepository = workflowDefinitionRepository;
    }

    /// <summary>
    /// 編集可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateEditAsync(
        Guid applicationId,
        Guid editorId,
        CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application == null)
            return BoundaryDecision.Deny("申請が見つかりません");

        return application.CanEdit(editorId);
    }

    /// <summary>
    /// 提出可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateSubmitAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application == null)
            return BoundaryDecision.Deny("申請が見つかりません");

        // ワークフロー定義の存在確認
        var workflowDefinition = await _workflowDefinitionRepository.GetByApplicationTypeAsync(application.Type, ct);
        if (workflowDefinition == null)
            return BoundaryDecision.Deny("この申請タイプに対応するワークフロー定義がありません");

        return application.CanSubmit();
    }

    /// <summary>
    /// 再提出可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateResubmitAsync(
        Guid applicationId,
        Guid applicantId,
        CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application == null)
            return BoundaryDecision.Deny("申請が見つかりません");

        return application.CanResubmit(applicantId);
    }

    /// <summary>
    /// 承認可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateApproveAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application == null)
            return BoundaryDecision.Deny("申請が見つかりません");

        // 基本的な状態チェック
        var basicCheck = application.CanApprove(approverId, approverRole);
        if (!basicCheck.IsAllowed)
            return basicCheck;

        // ワークフロー定義との照合
        if (application.WorkflowDefinitionId == null)
            return BoundaryDecision.Deny("ワークフロー定義が設定されていません");

        var workflowDefinition = await _workflowDefinitionRepository.GetByIdWithStepsAsync(
            application.WorkflowDefinitionId.Value, ct);
        if (workflowDefinition == null)
            return BoundaryDecision.Deny("ワークフロー定義が見つかりません");

        // 現在のステップでこのロールが承認できるかチェック
        if (!workflowDefinition.CanApproveAtStep(application.CurrentStepNumber, approverRole))
            return BoundaryDecision.Deny($"このステップでの承認権限がありません（必要なロール: {workflowDefinition.GetStep(application.CurrentStepNumber)?.Role ?? "不明"}）");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 却下可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateRejectAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default)
    {
        // 承認と同じ条件でチェック
        return await ValidateApproveAsync(applicationId, approverId, approverRole, ct);
    }

    /// <summary>
    /// 差し戻し可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateReturnAsync(
        Guid applicationId,
        Guid approverId,
        string approverRole,
        CancellationToken ct = default)
    {
        // 承認と同じ条件でチェック
        return await ValidateApproveAsync(applicationId, approverId, approverRole, ct);
    }

    /// <summary>
    /// 閲覧可否を判定
    /// </summary>
    public async Task<BoundaryDecision> ValidateViewAsync(
        Guid applicationId,
        Guid userId,
        string userRole,
        CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application == null)
            return BoundaryDecision.Deny("申請が見つかりません");

        return application.CanView(userId, userRole);
    }
}
