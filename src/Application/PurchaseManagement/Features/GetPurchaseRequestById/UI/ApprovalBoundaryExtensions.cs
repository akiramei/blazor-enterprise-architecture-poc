using GetPurchaseRequestById.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;
using Shared.Kernel;

namespace GetPurchaseRequestById.UI;

/// <summary>
/// 承認バウンダリー拡張メソッド：DTO対応
/// UIがドメインエンティティを直接扱えない場合の代替手段
/// </summary>
public static class ApprovalBoundaryExtensions
{
    /// <summary>
    /// DTOから承認資格をチェック
    /// ドメインロジックをDTO層にも適用可能にする
    /// </summary>
    public static ApprovalEligibility CheckEligibilityFromDto(
        this IApprovalBoundary boundary,
        PurchaseRequestDetailDto dto,
        Guid currentUserId)
    {
        if (dto == null)
            return ApprovalEligibility.NotEligible(
                new DomainError("REQUEST_NOT_FOUND", "購買申請が見つかりません")
            );

        if (currentUserId == Guid.Empty)
            return ApprovalEligibility.NotEligible(
                new DomainError("USER_NOT_AUTHENTICATED", "ユーザーが認証されていません")
            );

        // 終端状態チェック
        var status = (PurchaseRequestStatus)dto.Status;
        if (IsTerminalState(status))
            return ApprovalEligibility.NotEligible(
                new DomainError("TERMINAL_STATE", $"この申請は既に処理済みです（{status}）")
            );

        // 現在の承認ステップを取得（Status = 0 = Pending）
        var currentStep = dto.ApprovalSteps.FirstOrDefault(s => s.Status == 0);
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
    /// DTOから承認コンテキストを取得
    ///
    /// 【リファクタリング: Type-Safe Boundary Decision】
    /// string[] AllowedActions → BoundaryDecision with ApprovalAction[]
    /// </summary>
    public static ApprovalContextDto GetContextFromDto(
        this IApprovalBoundary boundary,
        PurchaseRequestDetailDto dto,
        Guid currentUserId)
    {
        var eligibility = boundary.CheckEligibilityFromDto(dto, currentUserId);

        var currentStep = dto.ApprovalSteps.FirstOrDefault(s => s.Status == 0);
        var completedSteps = dto.ApprovalSteps.Where(s => s.Status != 0).ToArray();
        var remainingSteps = dto.ApprovalSteps.Where(s => s.Status == 0 && s != currentStep).ToArray();

        var status = (PurchaseRequestStatus)dto.Status;
        var decision = DetermineBoundaryDecision(status, eligibility, dto.RequesterId, currentUserId, dto.Id);

        // 【UIポリシープッシュ】UIメタデータを生成
        var uiMetadata = UIMetadata.ForRequestStatus(status);
        var stepUIMetadata = GenerateStepUIMetadataFromDto(dto.ApprovalSteps);

        return new ApprovalContextDto
        {
            RequestId = dto.Id,
            CurrentStep = currentStep,
            CompletedSteps = completedSteps,
            RemainingSteps = remainingSteps,
            IsTerminalState = IsTerminalState(status),
            Decision = decision,
            StatusDisplay = StatusDisplayInfo.FromStatus(status),
            UIMetadata = uiMetadata,
            StepUIMetadata = stepUIMetadata
        };
    }

    /// <summary>
    /// 承認ステップごとのUIメタデータを生成（DTO版）
    /// </summary>
    private static Dictionary<int, UIMetadata> GenerateStepUIMetadataFromDto(IEnumerable<ApprovalStepDto> steps)
    {
        return steps.ToDictionary(
            step => step.StepNumber,
            step => UIMetadata.ForApprovalStep((ApprovalStepStatus)step.Status)
        );
    }

    /// <summary>
    /// バウンダリー判定を実行（型安全な許可/拒否判定）
    ///
    /// 【リファクタリング: Type-Safe Boundary Decision】
    /// string[] → ApprovalAction[] (UIメタデータを含む型安全な値オブジェクト)
    /// </summary>
    private static BoundaryDecision DetermineBoundaryDecision(
        PurchaseRequestStatus status,
        ApprovalEligibility eligibility,
        Guid requesterId,
        Guid currentUserId,
        Guid requestId)
    {
        var context = new DecisionContext
        {
            UserId = currentUserId,
            RequestId = requestId,
            RequestStatus = status,
            CurrentStepNumber = null, // DTOからは取得不可
            DecisionTimestamp = DateTime.UtcNow,
            IsRequester = requesterId == currentUserId,
            IsCurrentApprover = eligibility.CanApprove
        };

        // 拒否されている場合
        if (!eligibility.CanApprove && !eligibility.CanReject)
        {
            // キャンセル可能か判定
            bool canCancel = !IsTerminalState(status)
                && status != PurchaseRequestStatus.Draft
                && requesterId == currentUserId;

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
        if (!IsTerminalState(status)
            && status != PurchaseRequestStatus.Draft
            && requesterId == currentUserId)
        {
            actions.Add(ApprovalAction.Cancel());
        }

        return BoundaryDecision.Allowed(
            actions: actions,
            context: context
        );
    }

    private static bool IsTerminalState(PurchaseRequestStatus status)
    {
        return status is PurchaseRequestStatus.Approved
            or PurchaseRequestStatus.Rejected
            or PurchaseRequestStatus.Cancelled;
    }

    /// <summary>
    /// DTOからIntentContextを取得（Intent-Command分離パターン）
    /// UI層が業務意図（Intent）のみを扱えるようにする
    /// </summary>
    public static IntentContext GetIntentContextFromDto(
        this IApprovalBoundary boundary,
        PurchaseRequestDetailDto dto,
        Guid currentUserId)
    {
        var eligibility = boundary.CheckEligibilityFromDto(dto, currentUserId);
        var status = (PurchaseRequestStatus)dto.Status;
        var availableIntents = new List<AvailableIntent>();

        // 承認可能な場合、現在のステータスに応じたApproval Intentを追加
        if (eligibility.CanApprove)
        {
            var approvalIntent = DetermineApprovalIntent(status);
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

        // DTO版ではRequestは不要（nullでOK）
        return new IntentContext
        {
            Request = null, // DTO版ではnull
            AvailableIntents = availableIntents.ToArray(),
            CurrentUserId = currentUserId
        };
    }

    private static ApprovalIntent? DetermineApprovalIntent(PurchaseRequestStatus status)
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

/// <summary>
/// 承認コンテキスト（DTO版）
///
/// 【リファクタリング: Type-Safe Boundary Decision】
/// string[] AllowedActions → BoundaryDecision（型安全な判定結果）
///
/// 【UIポリシープッシュ】
/// UIメタデータをドメイン層からプッシュ
/// </summary>
public record ApprovalContextDto
{
    public required Guid RequestId { get; init; }
    public ApprovalStepDto? CurrentStep { get; init; }
    public ApprovalStepDto[] CompletedSteps { get; init; } = Array.Empty<ApprovalStepDto>();
    public ApprovalStepDto[] RemainingSteps { get; init; } = Array.Empty<ApprovalStepDto>();
    public bool IsTerminalState { get; init; }

    /// <summary>
    /// バウンダリー判定結果（型安全な許可/拒否情報）
    /// </summary>
    public required BoundaryDecision Decision { get; init; }

    public StatusDisplayInfo StatusDisplay { get; init; } = null!;

    /// <summary>
    /// UIメタデータ（UIポリシープッシュ）
    /// ドメイン層がUIの表示方法を指示
    /// </summary>
    public UIMetadata? UIMetadata { get; init; }

    /// <summary>
    /// 承認ステップごとのUIメタデータ（UIポリシープッシュ）
    /// Key: StepNumber, Value: UIメタデータ
    /// </summary>
    public IReadOnlyDictionary<int, UIMetadata>? StepUIMetadata { get; init; }

    /// <summary>
    /// 後方互換性: 許可されたアクション文字列配列
    /// 【Deprecated】Decision.AllowedActions を使用してください
    /// </summary>
    [Obsolete("Use Decision.AllowedActions instead. This property will be removed in future versions.")]
    public string[] AllowedActions => Decision.AllowedActions
        .Where(a => a.IsEnabled)
        .Select(a => a.Type.ToString())
        .ToArray();
}
