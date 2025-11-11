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
        var allowedActions = DetermineAllowedActions(status, eligibility, dto.RequesterId, currentUserId);

        return new ApprovalContextDto
        {
            RequestId = dto.Id,
            CurrentStep = currentStep,
            CompletedSteps = completedSteps,
            RemainingSteps = remainingSteps,
            IsTerminalState = IsTerminalState(status),
            AllowedActions = allowedActions,
            StatusDisplay = StatusDisplayInfo.FromStatus(status)
        };
    }

    private static string[] DetermineAllowedActions(
        PurchaseRequestStatus status,
        ApprovalEligibility eligibility,
        Guid requesterId,
        Guid currentUserId)
    {
        var actions = new List<string>();

        if (eligibility.CanApprove)
            actions.Add("Approve");

        if (eligibility.CanReject)
            actions.Add("Reject");

        // キャンセルは申請者のみ（承認済み・却下・キャンセル済みを除く）
        if (!IsTerminalState(status)
            && status != PurchaseRequestStatus.Draft
            && requesterId == currentUserId)
            actions.Add("Cancel");

        return actions.ToArray();
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
/// </summary>
public record ApprovalContextDto
{
    public required Guid RequestId { get; init; }
    public ApprovalStepDto? CurrentStep { get; init; }
    public ApprovalStepDto[] CompletedSteps { get; init; } = Array.Empty<ApprovalStepDto>();
    public ApprovalStepDto[] RemainingSteps { get; init; } = Array.Empty<ApprovalStepDto>();
    public bool IsTerminalState { get; init; }
    public string[] AllowedActions { get; init; } = Array.Empty<string>();
    public StatusDisplayInfo StatusDisplay { get; init; } = null!;
}
