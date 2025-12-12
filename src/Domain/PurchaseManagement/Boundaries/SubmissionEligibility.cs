namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 提出資格判定結果（ドメイン知識）
/// UIが"提出ボタンを活性化すべきか"の根拠を提供する
/// </summary>
public record SubmissionEligibility
{
    /// <summary>
    /// 提出可能か
    /// </summary>
    public bool CanSubmit { get; init; }

    /// <summary>
    /// 明細が存在するか
    /// </summary>
    public bool HasItems { get; init; }

    /// <summary>
    /// 金額上限内か
    /// </summary>
    public bool IsWithinAmountLimit { get; init; }

    /// <summary>
    /// 現在の合計金額
    /// </summary>
    public decimal CurrentTotal { get; init; }

    /// <summary>
    /// 許可される最大金額
    /// </summary>
    public decimal MaxAllowed { get; init; }

    /// <summary>
    /// ブロック理由（提出できない理由）
    /// </summary>
    public global::Shared.Kernel.DomainError[] BlockingReasons { get; init; } = Array.Empty<global::Shared.Kernel.DomainError>();

    /// <summary>
    /// 提出不可の場合のインスタンス作成
    /// </summary>
    public static SubmissionEligibility NotEligible(
        decimal currentTotal,
        decimal maxAllowed,
        params global::Shared.Kernel.DomainError[] reasons)
    {
        return new SubmissionEligibility
        {
            CanSubmit = false,
            HasItems = false,
            IsWithinAmountLimit = currentTotal <= maxAllowed,
            CurrentTotal = currentTotal,
            MaxAllowed = maxAllowed,
            BlockingReasons = reasons
        };
    }

    /// <summary>
    /// 提出可能な場合のインスタンス作成
    /// </summary>
    public static SubmissionEligibility Eligible(
        decimal currentTotal,
        decimal maxAllowed,
        bool hasItems)
    {
        return new SubmissionEligibility
        {
            CanSubmit = true,
            HasItems = hasItems,
            IsWithinAmountLimit = currentTotal <= maxAllowed,
            CurrentTotal = currentTotal,
            MaxAllowed = maxAllowed,
            BlockingReasons = Array.Empty<global::Shared.Kernel.DomainError>()
        };
    }
}
