using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 承認バウンダリーサービス：ドメインサービスとして承認ロジックを提供
/// </summary>
public class ApprovalBoundaryService : IApprovalBoundary
{
    /// <summary>
    /// 承認資格をチェック
    /// </summary>
    public ApprovalEligibility CheckEligibility(PurchaseRequest request, Guid currentUserId)
    {
        if (request == null)
            return ApprovalEligibility.NotEligible(
                new DomainError("REQUEST_NOT_FOUND", "購買申請が見つかりません")
            );

        if (currentUserId == Guid.Empty)
            return ApprovalEligibility.NotEligible(
                new DomainError("USER_NOT_AUTHENTICATED", "ユーザーが認証されていません")
            );

        // 終端状態チェック
        if (IsTerminalState(request.Status))
            return ApprovalEligibility.NotEligible(
                new DomainError("TERMINAL_STATE", $"この申請は既に処理済みです（{request.Status}）")
            );

        // 現在の承認ステップを取得
        var currentStep = request.CurrentApprovalStep;
        if (currentStep == null)
            return ApprovalEligibility.NotEligible(
                new DomainError("NO_PENDING_STEP", "承認待ちのステップがありません")
            );

        // 承認者チェック（SECURITY: 重要）
        if (currentStep.ApproverId != currentUserId)
            return ApprovalEligibility.NotEligible(
                new DomainError(
                    "NOT_ASSIGNED_APPROVER",
                    $"あなたはこのステップの承認者ではありません（承認者: {currentStep.ApproverName}）"
                )
            );

        // すべてのチェックをパス
        return ApprovalEligibility.Eligible(
            currentStep.ApproverId,
            currentStep.StepNumber
        );
    }

    /// <summary>
    /// 承認コンテキストを取得
    /// </summary>
    public ApprovalContext GetContext(PurchaseRequest request, Guid currentUserId)
    {
        var eligibility = CheckEligibility(request, currentUserId);

        var completedSteps = request.ApprovalSteps
            .Where(s => s.Status != ApprovalStepStatus.Pending)
            .OrderBy(s => s.StepNumber)
            .ToArray();

        var remainingSteps = request.ApprovalSteps
            .Where(s => s.Status == ApprovalStepStatus.Pending && s != request.CurrentApprovalStep)
            .OrderBy(s => s.StepNumber)
            .ToArray();

        var allowedActions = DetermineAllowedActions(request, eligibility);

        return new ApprovalContext
        {
            Request = request,
            CurrentStep = request.CurrentApprovalStep,
            CompletedSteps = completedSteps,
            RemainingSteps = remainingSteps,
            IsTerminalState = IsTerminalState(request.Status),
            AllowedActions = allowedActions,
            StatusDisplay = StatusDisplayInfo.FromStatus(request.Status)
        };
    }

    /// <summary>
    /// 許可されたアクションを判定
    /// </summary>
    private string[] DetermineAllowedActions(PurchaseRequest request, ApprovalEligibility eligibility)
    {
        var actions = new List<string>();

        if (eligibility.CanApprove)
            actions.Add("Approve");

        if (eligibility.CanReject)
            actions.Add("Reject");

        // キャンセルは申請者のみ（承認済み・却下・キャンセル済みを除く）
        if (!IsTerminalState(request.Status) && request.Status != PurchaseRequestStatus.Draft)
            actions.Add("Cancel");

        return actions.ToArray();
    }

    /// <summary>
    /// 終端状態（遷移不可能な状態）かチェック
    /// </summary>
    private bool IsTerminalState(PurchaseRequestStatus status)
    {
        return status is PurchaseRequestStatus.Approved
            or PurchaseRequestStatus.Rejected
            or PurchaseRequestStatus.Cancelled;
    }

    /// <summary>
    /// 実行可能なIntent一覧を取得（Intent-Command分離パターン）
    /// </summary>
    public IntentContext GetIntentContext(PurchaseRequest request, Guid currentUserId)
    {
        var eligibility = CheckEligibility(request, currentUserId);
        var availableIntents = new List<AvailableIntent>();

        // 承認可能な場合、現在のステータスに応じたApproval Intentを追加
        if (eligibility.CanApprove)
        {
            var approvalIntent = DetermineApprovalIntent(request.Status);
            if (approvalIntent.HasValue)
            {
                availableIntents.Add(AvailableIntent.Enabled(approvalIntent.Value));
            }
        }

        // 却下可能な場合、差し戻しと永久却下の両方を追加
        if (eligibility.CanReject)
        {
            availableIntents.Add(AvailableIntent.Enabled(ApprovalIntent.SendBackForRevision));
            availableIntents.Add(AvailableIntent.Enabled(ApprovalIntent.RejectPermanently));
        }

        return new IntentContext
        {
            Request = request, // エンティティ版では設定
            AvailableIntents = availableIntents.ToArray(),
            CurrentUserId = currentUserId
        };
    }

    /// <summary>
    /// 指定されたIntentが実行可能か判定
    /// </summary>
    public bool CanExecuteIntent(PurchaseRequest request, ApprovalIntent intent, Guid currentUserId)
    {
        var intentContext = GetIntentContext(request, currentUserId);
        return intentContext.CanExecute(intent);
    }

    /// <summary>
    /// IntentをMediatRコマンドに変換（Intent→Command マッピング）
    /// ドメイン層が業務意図を技術実装に変換する責務を持つ
    /// </summary>
    public object CreateCommandFromIntent(ApprovalIntent intent, Guid requestId, Guid userId, string? comment)
    {
        return intent switch
        {
            ApprovalIntent.PerformFirstApproval => CreateApproveCommand(requestId, comment ?? "1次承認"),
            ApprovalIntent.PerformSecondApproval => CreateApproveCommand(requestId, comment ?? "2次承認"),
            ApprovalIntent.PerformFinalApproval => CreateApproveCommand(requestId, comment ?? "最終承認"),
            ApprovalIntent.SendBackForRevision => CreateRejectCommand(requestId, comment ?? "修正のため差し戻し"),
            ApprovalIntent.RejectPermanently => CreateRejectCommand(requestId, comment ?? "申請を却下"),
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "未知のIntent")
        };
    }

    /// <summary>
    /// ステータスから適切なApproval Intentを判定
    /// </summary>
    private ApprovalIntent? DetermineApprovalIntent(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.PendingFirstApproval => ApprovalIntent.PerformFirstApproval,
            PurchaseRequestStatus.PendingSecondApproval => ApprovalIntent.PerformSecondApproval,
            PurchaseRequestStatus.PendingFinalApproval => ApprovalIntent.PerformFinalApproval,
            _ => null
        };
    }

    /// <summary>
    /// 承認コマンドを生成（リフレクション回避のため動的生成）
    /// </summary>
    private object CreateApproveCommand(Guid requestId, string comment)
    {
        // NOTE: コマンドはFeature層に定義されているため、ここでは動的生成
        // 将来的にはコマンドのファクトリーパターンを検討
        var commandType = Type.GetType("ApprovePurchaseRequest.Application.ApprovePurchaseRequestCommand, ApprovePurchaseRequest.Application");
        if (commandType == null)
            throw new InvalidOperationException("ApprovePurchaseRequestCommand型が見つかりません");

        return Activator.CreateInstance(commandType, new object[] { requestId, comment, Guid.NewGuid().ToString() })
            ?? throw new InvalidOperationException("コマンドの生成に失敗しました");
    }

    /// <summary>
    /// 却下コマンドを生成（リフレクション回避のため動的生成）
    /// </summary>
    private object CreateRejectCommand(Guid requestId, string reason)
    {
        var commandType = Type.GetType("RejectPurchaseRequest.Application.RejectPurchaseRequestCommand, RejectPurchaseRequest.Application");
        if (commandType == null)
            throw new InvalidOperationException("RejectPurchaseRequestCommand型が見つかりません");

        return Activator.CreateInstance(commandType, new object[] { requestId, reason, Guid.NewGuid().ToString() })
            ?? throw new InvalidOperationException("コマンドの生成に失敗しました");
    }
}
