namespace Domain.PurchaseManagement.PurchaseRequests.Boundaries;

/// <summary>
/// 承認資格判定結果（ドメイン知識）
/// UIが"承認ボタンを活性化すべきか"の根拠を提供する
/// </summary>
public record ApprovalEligibility
{
    /// <summary>
    /// 承認可能か
    /// </summary>
    public bool CanApprove { get; init; }

    /// <summary>
    /// 却下可能か
    /// </summary>
    public bool CanReject { get; init; }

    /// <summary>
    /// 現在のステップの承認者ID（承認権限判定用）
    /// </summary>
    public Guid? CurrentStepApproverId { get; init; }

    /// <summary>
    /// 現在のステップ番号
    /// </summary>
    public int? CurrentStepNumber { get; init; }

    /// <summary>
    /// ブロック理由（承認/却下できない理由）
    /// </summary>
    public global::Shared.Kernel.DomainError[] BlockingReasons { get; init; } = Array.Empty<global::Shared.Kernel.DomainError>();

    /// <summary>
    /// 承認不可の場合のインスタンス作成
    /// </summary>
    public static ApprovalEligibility NotEligible(params global::Shared.Kernel.DomainError[] reasons)
    {
        return new ApprovalEligibility
        {
            CanApprove = false,
            CanReject = false,
            BlockingReasons = reasons
        };
    }

    /// <summary>
    /// 承認・却下可能な場合のインスタンス作成
    /// </summary>
    public static ApprovalEligibility Eligible(Guid currentStepApproverId, int currentStepNumber)
    {
        return new ApprovalEligibility
        {
            CanApprove = true,
            CanReject = true,
            CurrentStepApproverId = currentStepApproverId,
            CurrentStepNumber = currentStepNumber,
            BlockingReasons = Array.Empty<global::Shared.Kernel.DomainError>()
        };
    }
}
