using Shared.Kernel;

namespace Domain.ApprovalWorkflow.Applications;

/// <summary>
/// 承認履歴エントリ（エンティティ）
///
/// 【パターン: Approval History Pattern】
///
/// 責務:
/// - 個別の承認アクション（承認/却下/差し戻し）の記録
/// - 承認者情報の保持
/// - 不変性の保証（Append Only）
///
/// 不変条件:
/// - 承認履歴は不変。後から上書きしない（Append Only）
/// </summary>
public sealed class ApprovalHistoryEntry : Entity
{
    /// <summary>履歴ID</summary>
    public Guid Id { get; private set; }

    /// <summary>申請ID</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>承認ステップ番号</summary>
    public int StepNumber { get; private set; }

    /// <summary>承認者ID</summary>
    public Guid ApproverId { get; private set; }

    /// <summary>アクション（Approved / Rejected / Returned）</summary>
    public ApprovalAction Action { get; private set; }

    /// <summary>コメント（却下/差し戻しの場合は理由）</summary>
    public string? Comment { get; private set; }

    /// <summary>アクション実行日時</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private ApprovalHistoryEntry() { }

    private ApprovalHistoryEntry(
        Guid applicationId,
        int stepNumber,
        Guid approverId,
        ApprovalAction action,
        string? comment)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        StepNumber = stepNumber;
        ApproverId = approverId;
        Action = action;
        Comment = comment;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>承認の履歴を作成</summary>
    public static ApprovalHistoryEntry CreateApproved(
        Guid applicationId,
        int stepNumber,
        Guid approverId,
        string? comment = null)
    {
        return new ApprovalHistoryEntry(
            applicationId, stepNumber, approverId, ApprovalAction.Approved, comment);
    }

    /// <summary>却下の履歴を作成</summary>
    public static ApprovalHistoryEntry CreateRejected(
        Guid applicationId,
        int stepNumber,
        Guid approverId,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("却下理由は必須です");

        return new ApprovalHistoryEntry(
            applicationId, stepNumber, approverId, ApprovalAction.Rejected, reason);
    }

    /// <summary>差し戻しの履歴を作成</summary>
    public static ApprovalHistoryEntry CreateReturned(
        Guid applicationId,
        int stepNumber,
        Guid approverId,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("差し戻し理由は必須です");

        return new ApprovalHistoryEntry(
            applicationId, stepNumber, approverId, ApprovalAction.Returned, reason);
    }
}

/// <summary>
/// 承認アクションの種類
/// </summary>
public enum ApprovalAction
{
    /// <summary>承認</summary>
    Approved = 0,

    /// <summary>却下</summary>
    Rejected = 1,

    /// <summary>差し戻し</summary>
    Returned = 2
}
