using Microsoft.Extensions.Logging;
using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Boundaries;

/// <summary>
/// 承認バウンダリーサービス：ドメインサービスとして承認ロジックを提供
///
/// 【可観測性】
/// CheckEligibility, GetContext の呼び出しを構造化ログで記録
/// ビジネス指標（拒否率、よく使われるアクション）を可視化可能
/// </summary>
public class ApprovalBoundaryService : IApprovalBoundary
{
    private readonly IApprovalCommandFactory _commandFactory;
    private readonly ILogger<ApprovalBoundaryService> _logger;

    public ApprovalBoundaryService(
        IApprovalCommandFactory commandFactory,
        ILogger<ApprovalBoundaryService> logger)
    {
        _commandFactory = commandFactory;
        _logger = logger;
    }
    /// <summary>
    /// 承認資格をチェック
    ///
    /// 【可観測性】構造化ログでチェック結果を記録
    /// </summary>
    public ApprovalEligibility CheckEligibility(PurchaseRequest request, Guid currentUserId)
    {
        if (request == null)
        {
            _logger.LogWarning("CheckEligibility failed: REQUEST_NOT_FOUND for userId={UserId}",
                currentUserId);
            return ApprovalEligibility.NotEligible(
                new DomainError("REQUEST_NOT_FOUND", "購買申請が見つかりません")
            );
        }

        if (currentUserId == Guid.Empty)
        {
            _logger.LogWarning("CheckEligibility failed: USER_NOT_AUTHENTICATED for requestId={RequestId}",
                request.Id);
            return ApprovalEligibility.NotEligible(
                new DomainError("USER_NOT_AUTHENTICATED", "ユーザーが認証されていません")
            );
        }

        // 終端状態チェック
        if (IsTerminalState(request.Status))
        {
            _logger.LogInformation(
                "CheckEligibility denied: TERMINAL_STATE. RequestId={RequestId}, Status={Status}, UserId={UserId}",
                request.Id, request.Status, currentUserId);
            return ApprovalEligibility.NotEligible(
                new DomainError("TERMINAL_STATE", $"この申請は既に処理済みです（{request.Status}）")
            );
        }

        // 現在の承認ステップを取得
        var currentStep = request.CurrentApprovalStep;
        if (currentStep == null)
        {
            _logger.LogWarning(
                "CheckEligibility denied: NO_PENDING_STEP. RequestId={RequestId}, Status={Status}, UserId={UserId}",
                request.Id, request.Status, currentUserId);
            return ApprovalEligibility.NotEligible(
                new DomainError("NO_PENDING_STEP", "承認待ちのステップがありません")
            );
        }

        // 承認者チェック（SECURITY: 重要）
        if (currentStep.ApproverId != currentUserId)
        {
            _logger.LogWarning(
                "CheckEligibility denied: NOT_ASSIGNED_APPROVER. RequestId={RequestId}, ExpectedApproverId={ExpectedApproverId}, ActualUserId={ActualUserId}, StepNumber={StepNumber}",
                request.Id, currentStep.ApproverId, currentUserId, currentStep.StepNumber);
            return ApprovalEligibility.NotEligible(
                new DomainError(
                    "NOT_ASSIGNED_APPROVER",
                    $"あなたはこのステップの承認者ではありません（承認者: {currentStep.ApproverName}）"
                )
            );
        }

        // すべてのチェックをパス
        _logger.LogInformation(
            "CheckEligibility approved. RequestId={RequestId}, UserId={UserId}, StepNumber={StepNumber}",
            request.Id, currentUserId, currentStep.StepNumber);

        return ApprovalEligibility.Eligible(
            currentStep.ApproverId,
            currentStep.StepNumber
        );
    }

    /// <summary>
    /// 承認コンテキストを取得
    ///
    /// 【リファクタリング: Type-Safe Boundary Decision】
    /// string[] AllowedActions → BoundaryDecision with ApprovalAction[]
    ///
    /// 【可観測性】構造化ログでコンテキスト取得と判定結果を記録
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

        var decision = DetermineBoundaryDecision(request, eligibility, currentUserId);

        // 【可観測性】判定結果をログに記録
        _logger.LogInformation(
            "GetContext completed. RequestId={RequestId}, UserId={UserId}, Status={Status}, " +
            "IsAllowed={IsAllowed}, AllowedActionsCount={AllowedActionsCount}, BlockingReasonsCount={BlockingReasonsCount}, " +
            "AllowedActions={AllowedActions}",
            request.Id,
            currentUserId,
            request.Status,
            decision.IsAllowed,
            decision.AllowedActions.Count,
            decision.BlockingReasons.Count,
            string.Join(",", decision.AllowedActions.Select(a => a.Type.ToString()))
        );

        // 【UIポリシープッシュ】UIメタデータを生成
        var uiMetadata = UIMetadata.ForRequestStatus(request.Status);
        var stepUIMetadata = GenerateStepUIMetadata(request.ApprovalSteps);

        return new ApprovalContext
        {
            Request = request,
            CurrentStep = request.CurrentApprovalStep,
            CompletedSteps = completedSteps,
            RemainingSteps = remainingSteps,
            IsTerminalState = IsTerminalState(request.Status),
            Decision = decision,
            StatusDisplay = StatusDisplayInfo.FromStatus(request.Status),
            UIMetadata = uiMetadata,
            StepUIMetadata = stepUIMetadata
        };
    }

    /// <summary>
    /// 承認ステップごとのUIメタデータを生成
    /// </summary>
    private Dictionary<int, UIMetadata> GenerateStepUIMetadata(IEnumerable<ApprovalStep> steps)
    {
        return steps.ToDictionary(
            step => step.StepNumber,
            step => UIMetadata.ForApprovalStep(step.Status)
        );
    }

    /// <summary>
    /// バウンダリー判定を実行（型安全な許可/拒否判定）
    ///
    /// 【SECURITY FIX】
    /// Cancel アクションは申請者（RequesterId）のみに許可する。
    /// 修正前は currentUserId をチェックせず、全ユーザーに Cancel を表示していた。
    ///
    /// 【リファクタリング: Type-Safe Boundary Decision】
    /// string[] → ApprovalAction[] (UIメタデータを含む型安全な値オブジェクト)
    /// </summary>
    private BoundaryDecision DetermineBoundaryDecision(
        PurchaseRequest request,
        ApprovalEligibility eligibility,
        Guid currentUserId)
    {
        var context = DecisionContext.Create(
            userId: currentUserId,
            request: request,
            isRequester: request.RequesterId == currentUserId,
            isCurrentApprover: request.CurrentApprovalStep?.ApproverId == currentUserId
        );

        // 拒否されている場合
        if (!eligibility.CanApprove && !eligibility.CanReject)
        {
            // キャンセル可能か判定
            bool canCancel = !IsTerminalState(request.Status)
                && request.Status != PurchaseRequestStatus.Draft
                && request.RequesterId == currentUserId;

            if (!canCancel)
            {
                // 何もできない場合は拒否
                return BoundaryDecision.Denied(
                    reasons: eligibility.BlockingReasons.ToList(),
                    context: context
                );
            }

            // キャンセルのみ可能
            return BoundaryDecision.Allowed(
                actions: new[] { ApprovalAction.Cancel() },
                context: context
            );
        }

        // 許可されたアクションを構築
        var actions = new List<ApprovalAction>();

        if (eligibility.CanApprove)
            actions.Add(ApprovalAction.Approve());

        if (eligibility.CanReject)
            actions.Add(ApprovalAction.Reject());

        // SECURITY: キャンセルは申請者のみ（承認済み・却下・キャンセル済みを除く）
        if (!IsTerminalState(request.Status)
            && request.Status != PurchaseRequestStatus.Draft
            && request.RequesterId == currentUserId) // SECURITY FIX: 申請者チェック追加
        {
            actions.Add(ApprovalAction.Cancel());
        }

        return BoundaryDecision.Allowed(
            actions: actions,
            context: context
        );
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
    ///
    /// 【リファクタリング: リフレクション削除】
    /// Abstract Factoryパターンを使い、型安全にコマンドを生成
    /// </summary>
    public object CreateCommandFromIntent(ApprovalIntent intent, Guid requestId, Guid userId, string? comment, string idempotencyKey)
    {
        return intent switch
        {
            ApprovalIntent.PerformFirstApproval =>
                _commandFactory.CreateApproveCommand(requestId, comment ?? "1次承認", idempotencyKey),
            ApprovalIntent.PerformSecondApproval =>
                _commandFactory.CreateApproveCommand(requestId, comment ?? "2次承認", idempotencyKey),
            ApprovalIntent.PerformFinalApproval =>
                _commandFactory.CreateApproveCommand(requestId, comment ?? "最終承認", idempotencyKey),
            ApprovalIntent.SendBackForRevision =>
                _commandFactory.CreateRejectCommand(requestId, comment ?? "修正のため差し戻し", idempotencyKey),
            ApprovalIntent.RejectPermanently =>
                _commandFactory.CreateRejectCommand(requestId, comment ?? "申請を却下", idempotencyKey),
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
}
