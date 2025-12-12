using Domain.PurchaseManagement;
using Domain.PurchaseManagement.PurchaseRequests.Events;
using Domain.PurchaseManagement.PurchaseRequests.StateMachine;
using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests;

/// <summary>
/// 購買申請（集約ルート）
/// </summary>
public class PurchaseRequest : AggregateRoot<Guid>, IMultiTenant
{
    private readonly PurchaseRequestStateMachine _stateMachine = new();
    private readonly List<ApprovalStep> _approvalSteps = new();
    private readonly List<PurchaseRequestItem> _items = new();
    private readonly List<PurchaseRequestAttachment> _attachments = new();

    // マルチテナント対応
    public Guid TenantId { get; private set; }

    // 基本情報
    public PurchaseRequestNumber RequestNumber { get; private set; } = null!;
    public Guid RequesterId { get; private set; }
    public string RequesterName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    // 状態
    public PurchaseRequestStatus Status { get; private set; }

    // 日時
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // 承認情報
    public IReadOnlyList<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();
    public ApprovalStep? CurrentApprovalStep => _approvalSteps
        .Where(s => s.IsPending)
        .OrderBy(s => s.StepNumber)
        .FirstOrDefault();

    // 明細
    public IReadOnlyList<PurchaseRequestItem> Items => _items.AsReadOnly();
    public Money TotalAmount => new(_items.Sum(i => i.Amount.Amount));

    // 添付ファイル
    public IReadOnlyList<PurchaseRequestAttachment> Attachments => _attachments.AsReadOnly();

    // ビジネスルール: 金額制限
    private const decimal MaxRequestAmount = 1_000_000m; // 100万円

    /// <summary>
    /// パラメータレスコンストラクタ
    /// オブジェクトの再構成時に使用（デシリアライズ、マッピング等）
    /// </summary>
    private PurchaseRequest()
    {
        // コレクションの初期化（ドメインルール）
        _approvalSteps = new List<ApprovalStep>();
        _items = new List<PurchaseRequestItem>();
        _attachments = new List<PurchaseRequestAttachment>();
    }

    #region ファクトリメソッド

    /// <summary>
    /// 購買申請を作成（下書き状態）
    /// </summary>
    public static PurchaseRequest Create(
        Guid requesterId,
        string requesterName,
        string title,
        string description,
        Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("タイトルは必須です");

        if (tenantId == Guid.Empty)
            throw new DomainException("TenantIdは必須です");

        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = PurchaseRequestNumber.Generate(),
            RequesterId = requesterId,
            RequesterName = requesterName,
            Title = title,
            Description = description,
            TenantId = tenantId,
            Status = PurchaseRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return request;
    }

    #endregion

    #region 明細操作

    /// <summary>
    /// 明細を追加
    /// </summary>
    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new DomainException("明細の追加は下書き状態でのみ可能です");

        var item = PurchaseRequestItem.Create(productId, productName, unitPrice, quantity);
        _items.Add(item);

        // 金額上限チェック
        if (TotalAmount.Amount > MaxRequestAmount)
        {
            _items.Remove(item); // ロールバック
            throw new DomainException($"購買申請の合計金額は{MaxRequestAmount:N0}円までです");
        }
    }

    /// <summary>
    /// 明細を削除
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new DomainException("明細の削除は下書き状態でのみ可能です");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new DomainException("明細が見つかりません");

        _items.Remove(item);
    }

    #endregion

    #region 添付ファイル操作

    /// <summary>
    /// 添付ファイルを追加
    /// </summary>
    public void AddAttachment(PurchaseRequestAttachment attachment)
    {
        if (attachment == null)
            throw new ArgumentNullException(nameof(attachment));

        if (attachment.PurchaseRequestId != Id)
            throw new DomainException("この購買申請の添付ファイルではありません");

        if (attachment.TenantId != TenantId)
            throw new DomainException("異なるテナントの添付ファイルは追加できません");

        _attachments.Add(attachment);
    }

    /// <summary>
    /// 添付ファイルを削除（論理削除）
    /// </summary>
    public void RemoveAttachment(Guid attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
            throw new DomainException("添付ファイルが見つかりません");

        attachment.Delete();
    }

    #endregion

    #region ワークフロー操作

    /// <summary>
    /// 申請提出
    /// </summary>
    public void Submit(ApprovalFlow approvalFlow)
    {
        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Submitted);

        if (_items.Count == 0)
            throw new DomainException("明細が1件もありません");

        // 承認フローを設定
        foreach (var step in approvalFlow.Steps)
        {
            _approvalSteps.Add(new ApprovalStep(
                step.StepNumber,
                step.ApproverId,
                step.ApproverName,
                step.ApproverRole
            ));
        }

        // 状態遷移: Draft → Submitted → PendingFirstApproval
        Status = PurchaseRequestStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        // 承認フローがある場合、1次承認待ちに遷移
        if (_approvalSteps.Count > 0)
        {
            _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.PendingFirstApproval);
            Status = PurchaseRequestStatus.PendingFirstApproval;
        }

        // Domain Event発行
        RaiseDomainEvent(new PurchaseRequestSubmittedEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            RequesterName,
            TotalAmount.Amount,
            SubmittedAt.Value
        ));
    }

    /// <summary>
    /// 承認
    /// </summary>
    public void Approve(Guid approverId, string comment)
    {
        // 現在の承認ステップを取得
        var currentStep = CurrentApprovalStep;
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        // 承認者チェック
        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        // 承認ステップを完了
        currentStep.Approve(comment);

        // 次のステップがあるか確認
        var nextStep = _approvalSteps.FirstOrDefault(s => s.StepNumber == currentStep.StepNumber + 1);
        if (nextStep is not null)
        {
            // 次の承認ステップへ
            var nextStatus = GetNextApprovalStatus(nextStep.StepNumber);
            _stateMachine.ValidateTransition(Status, nextStatus);
            Status = nextStatus;
        }
        else
        {
            // 最終承認完了
            _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Approved);
            Status = PurchaseRequestStatus.Approved;
            ApprovedAt = DateTime.UtcNow;

            // Domain Event発行
            RaiseDomainEvent(new PurchaseRequestApprovedEvent(
                Id,
                RequestNumber.Value,
                RequesterId,
                approverId,
                TotalAmount.Amount,
                ApprovedAt.Value
            ));
        }
    }

    /// <summary>
    /// 却下
    /// </summary>
    public void Reject(Guid approverId, string reason)
    {
        var currentStep = CurrentApprovalStep;
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        // 承認ステップを却下
        currentStep.Reject(reason);

        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Rejected);
        Status = PurchaseRequestStatus.Rejected;
        RejectedAt = DateTime.UtcNow;

        // Domain Event発行
        RaiseDomainEvent(new PurchaseRequestRejectedEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            approverId,
            reason,
            RejectedAt.Value
        ));
    }

    /// <summary>
    /// キャンセル（申請者のみ可能）
    /// </summary>
    public void Cancel(Guid userId)
    {
        if (userId != RequesterId)
            throw new DomainException("申請者のみキャンセルできます");

        // キャンセル可能な状態かチェック
        if (Status == PurchaseRequestStatus.Draft)
            throw new DomainException("下書きはキャンセルできません（削除してください）");

        if (Status == PurchaseRequestStatus.Approved)
            throw new DomainException("承認済みの申請はキャンセルできません");

        if (Status == PurchaseRequestStatus.Rejected)
            throw new DomainException("既に却下されています");

        if (Status == PurchaseRequestStatus.Cancelled)
            throw new DomainException("既にキャンセルされています");

        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Cancelled);
        Status = PurchaseRequestStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        // Domain Event発行
        RaiseDomainEvent(new PurchaseRequestCancelledEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            CancelledAt.Value
        ));
    }

    #endregion

    #region ヘルパーメソッド

    private PurchaseRequestStatus GetNextApprovalStatus(int stepNumber)
    {
        return stepNumber switch
        {
            1 => PurchaseRequestStatus.PendingFirstApproval,
            2 => PurchaseRequestStatus.PendingSecondApproval,
            3 => PurchaseRequestStatus.PendingFinalApproval,
            _ => throw new DomainException($"無効なステップ番号: {stepNumber}")
        };
    }

    #endregion
}
