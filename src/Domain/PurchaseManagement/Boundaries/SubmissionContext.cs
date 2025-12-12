namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 提出コンテキスト：UIが"何を見せるべきか"の根拠
/// 提出画面に表示する情報の完全なスナップショット
/// </summary>
public record SubmissionContext
{
    /// <summary>
    /// タイトルが有効か
    /// </summary>
    public bool IsTitleValid { get; init; }

    /// <summary>
    /// 明細数
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// 合計金額
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 通貨
    /// </summary>
    public string Currency { get; init; } = "JPY";

    /// <summary>
    /// 金額上限
    /// </summary>
    public decimal MaxAllowedAmount { get; init; }

    /// <summary>
    /// 金額上限までの残り
    /// </summary>
    public decimal RemainingAmount => MaxAllowedAmount - TotalAmount;

    /// <summary>
    /// 金額が上限に近いか（警告表示用: 80%以上）
    /// </summary>
    public bool IsNearLimit => TotalAmount >= MaxAllowedAmount * 0.8m;

    /// <summary>
    /// 金額が上限を超えているか
    /// </summary>
    public bool IsOverLimit => TotalAmount > MaxAllowedAmount;

    /// <summary>
    /// 検証エラーメッセージ
    /// </summary>
    public string[] ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 提出可能か
    /// </summary>
    public bool CanSubmit { get; init; }
}
