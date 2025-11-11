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
}
