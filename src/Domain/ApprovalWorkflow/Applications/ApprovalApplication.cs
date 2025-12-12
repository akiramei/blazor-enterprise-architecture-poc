using Domain.ApprovalWorkflow.Boundaries;
using Domain.ApprovalWorkflow.Applications.Events;
using Domain.ApprovalWorkflow.Applications.StateMachine;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications;

/// <summary>
/// 申請（集約ルート）
///
/// 【パターン: Approval Workflow - Aggregate Root】
///
/// 責務:
/// - 申請のライフサイクル管理（Draft → Submitted → InReview → Approved/Rejected/Returned）
/// - 承認フローの実行と進行管理
/// - ビジネスルールの検証（CanXxx メソッド）
/// - ドメインイベントの発行
///
/// 不変条件:
/// - Draft以外の申請は編集不可
/// - 承認ステップは順番通りにしか進めない
/// - 該当ステップのロールを持っていないユーザーは承認不可
/// - Approved/Rejectedの申請は再提出不可
/// - Returnedの申請はApplicantのみが再提出可能
/// </summary>
public sealed class ApprovalApplication : AggregateRoot<Guid>
{
    private readonly ApplicationStateMachine _stateMachine = new();
    private readonly List<ApprovalHistoryEntry> _approvalHistory = new();

    /// <summary>申請者ID</summary>
    public Guid ApplicantId { get; private set; }

    /// <summary>申請タイプ</summary>
    public ApplicationType Type { get; private set; }

    /// <summary>申請内容（自由形式）</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>申請ステータス</summary>
    public ApplicationStatus Status { get; private set; }

    /// <summary>現在の承認ステップ番号（0 = 未開始、1以上 = 該当ステップ承認待ち）</summary>
    public int CurrentStepNumber { get; private set; }

    /// <summary>ワークフロー定義ID（提出時に設定）</summary>
    public Guid? WorkflowDefinitionId { get; private set; }

    /// <summary>作成日時</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>提出日時</summary>
    public DateTime? SubmittedAt { get; private set; }

    /// <summary>承認完了日時</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>却下日時</summary>
    public DateTime? RejectedAt { get; private set; }

    /// <summary>差し戻し日時</summary>
    public DateTime? ReturnedAt { get; private set; }

    /// <summary>承認履歴（読み取り専用）</summary>
    public IReadOnlyList<ApprovalHistoryEntry> ApprovalHistory => _approvalHistory.AsReadOnly();

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private ApprovalApplication() { }

    /// <summary>
    /// 申請を作成（下書き状態）
    /// </summary>
    public static ApprovalApplication Create(
        Guid applicantId,
        ApplicationType type,
        string content)
    {
        if (applicantId == Guid.Empty)
            throw new DomainException("申請者IDは必須です");

        var now = DateTime.UtcNow;
        var application = new ApprovalApplication
        {
            Id = Guid.NewGuid(),
            ApplicantId = applicantId,
            Type = type,
            Content = content,
            Status = ApplicationStatus.Draft,
            CurrentStepNumber = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        return application;
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// 申請内容を編集（Draft状態のみ）
    /// </summary>
    public void Edit(string newContent, Guid editorId)
    {
        var canEdit = CanEdit(editorId);
        if (!canEdit.IsAllowed)
            throw new DomainException(canEdit.Reason!);

        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 申請を提出
    /// </summary>
    public void Submit(WorkflowDefinition workflowDefinition)
    {
        var canSubmit = CanSubmit();
        if (!canSubmit.IsAllowed)
            throw new DomainException(canSubmit.Reason!);

        _stateMachine.ValidateTransition(Status, ApplicationStatus.Submitted);

        WorkflowDefinitionId = workflowDefinition.Id;
        Status = ApplicationStatus.Submitted;
        CurrentStepNumber = 1;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // 提出後すぐに承認待ち状態に遷移
        _stateMachine.ValidateTransition(Status, ApplicationStatus.InReview);
        Status = ApplicationStatus.InReview;

        RaiseDomainEvent(new ApplicationSubmittedEvent(Id, ApplicantId, Type, SubmittedAt.Value));
    }

    /// <summary>
    /// 再提出（差し戻し後）
    /// </summary>
    public void Resubmit(Guid applicantId)
    {
        var canResubmit = CanResubmit(applicantId);
        if (!canResubmit.IsAllowed)
            throw new DomainException(canResubmit.Reason!);

        _stateMachine.ValidateTransition(Status, ApplicationStatus.Submitted);
        Status = ApplicationStatus.Submitted;
        CurrentStepNumber = 1;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ReturnedAt = null;

        // 再提出後すぐに承認待ち状態に遷移
        _stateMachine.ValidateTransition(Status, ApplicationStatus.InReview);
        Status = ApplicationStatus.InReview;

        RaiseDomainEvent(new ApplicationResubmittedEvent(Id, ApplicantId, SubmittedAt.Value));
    }

    /// <summary>
    /// 承認
    /// </summary>
    public void Approve(Guid approverId, string approverRole, int totalSteps, string? comment = null)
    {
        var canApprove = CanApprove(approverId, approverRole);
        if (!canApprove.IsAllowed)
            throw new DomainException(canApprove.Reason!);

        // 承認履歴を記録
        _approvalHistory.Add(ApprovalHistoryEntry.CreateApproved(
            Id, CurrentStepNumber, approverId, comment));

        // 次のステップがあるか判定
        if (CurrentStepNumber < totalSteps)
        {
            // 次のステップへ
            CurrentStepNumber++;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ApplicationStepApprovedEvent(
                Id, approverId, CurrentStepNumber - 1, DateTime.UtcNow));
        }
        else
        {
            // 最終承認完了
            _stateMachine.ValidateTransition(Status, ApplicationStatus.Approved);
            Status = ApplicationStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ApplicationApprovedEvent(Id, ApplicantId, approverId, ApprovedAt.Value));
        }
    }

    /// <summary>
    /// 却下
    /// </summary>
    public void Reject(Guid approverId, string approverRole, string reason)
    {
        var canReject = CanReject(approverId, approverRole);
        if (!canReject.IsAllowed)
            throw new DomainException(canReject.Reason!);

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("却下理由は必須です");

        _stateMachine.ValidateTransition(Status, ApplicationStatus.Rejected);

        // 承認履歴を記録
        _approvalHistory.Add(ApprovalHistoryEntry.CreateRejected(
            Id, CurrentStepNumber, approverId, reason));

        Status = ApplicationStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApplicationRejectedEvent(Id, ApplicantId, approverId, reason, RejectedAt.Value));
    }

    /// <summary>
    /// 差し戻し
    /// </summary>
    public void Return(Guid approverId, string approverRole, string reason)
    {
        var canReturn = CanReturn(approverId, approverRole);
        if (!canReturn.IsAllowed)
            throw new DomainException(canReturn.Reason!);

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("差し戻し理由は必須です");

        _stateMachine.ValidateTransition(Status, ApplicationStatus.Returned);

        // 承認履歴を記録
        _approvalHistory.Add(ApprovalHistoryEntry.CreateReturned(
            Id, CurrentStepNumber, approverId, reason));

        Status = ApplicationStatus.Returned;
        CurrentStepNumber = 0;
        ReturnedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ApplicationReturnedEvent(Id, ApplicantId, approverId, reason, ReturnedAt.Value));
    }

    // ================================================================
    // 業務ルール判定メソッド（CanXxx）- BoundaryDecisionを返す
    // ================================================================

    /// <summary>
    /// 編集可否を判定
    /// </summary>
    public BoundaryDecision CanEdit(Guid editorId)
    {
        if (editorId != ApplicantId)
            return BoundaryDecision.Deny("申請者のみが編集できます");

        if (Status != ApplicationStatus.Draft)
            return BoundaryDecision.Deny("下書き状態の申請のみ編集できます");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 提出可否を判定
    /// </summary>
    public BoundaryDecision CanSubmit()
    {
        if (Status != ApplicationStatus.Draft)
            return BoundaryDecision.Deny("下書き状態の申請のみ提出できます");

        if (string.IsNullOrWhiteSpace(Content))
            return BoundaryDecision.Deny("申請内容が入力されていません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 再提出可否を判定
    /// </summary>
    public BoundaryDecision CanResubmit(Guid applicantId)
    {
        if (applicantId != ApplicantId)
            return BoundaryDecision.Deny("申請者のみが再提出できます");

        if (Status != ApplicationStatus.Returned)
            return BoundaryDecision.Deny("差し戻し状態の申請のみ再提出できます");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 承認可否を判定
    /// </summary>
    /// <param name="approverId">承認者ID</param>
    /// <param name="approverRole">承認者のロール</param>
    /// <remarks>
    /// ロールが現在のステップの承認権限を持っているかはワークフロー定義との照合が必要
    /// このメソッドでは基本的な状態チェックのみ行う
    /// </remarks>
    public BoundaryDecision CanApprove(Guid approverId, string approverRole)
    {
        if (Status != ApplicationStatus.InReview)
            return BoundaryDecision.Deny("承認待ち状態ではありません");

        if (CurrentStepNumber <= 0)
            return BoundaryDecision.Deny("承認ステップが開始されていません");

        // 申請者自身は承認できない
        if (approverId == ApplicantId)
            return BoundaryDecision.Deny("申請者は自分の申請を承認できません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 却下可否を判定
    /// </summary>
    public BoundaryDecision CanReject(Guid approverId, string approverRole)
    {
        // 承認と同じ条件
        return CanApprove(approverId, approverRole);
    }

    /// <summary>
    /// 差し戻し可否を判定
    /// </summary>
    public BoundaryDecision CanReturn(Guid approverId, string approverRole)
    {
        // 承認と同じ条件
        return CanApprove(approverId, approverRole);
    }

    /// <summary>
    /// 閲覧可否を判定
    /// </summary>
    public BoundaryDecision CanView(Guid userId, string userRole)
    {
        // 申請者は常に閲覧可能
        if (userId == ApplicantId)
            return BoundaryDecision.Allow();

        // Adminは全て閲覧可能
        if (userRole == "Admin")
            return BoundaryDecision.Allow();

        // 現在の承認ステップの担当者は閲覧可能
        // （実際のロールチェックはワークフロー定義との照合が必要）

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 次に遷移可能な状態を取得
    /// </summary>
    public IEnumerable<ApplicationStatus> GetAvailableStatusTransitions()
    {
        return _stateMachine.GetAllowedTransitions(Status);
    }
}
