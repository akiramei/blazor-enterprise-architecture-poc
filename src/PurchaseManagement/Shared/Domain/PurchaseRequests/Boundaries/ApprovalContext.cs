namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 承認コンテキスト：UIが"何を見せるべきか"の根拠
/// 承認画面に表示する情報の完全なスナップショット
/// </summary>
public record ApprovalContext
{
    /// <summary>
    /// 購買申請の基本情報
    /// </summary>
    public required PurchaseRequest Request { get; init; }

    /// <summary>
    /// 現在の承認ステップ（承認待ち）
    /// </summary>
    public ApprovalStep? CurrentStep { get; init; }

    /// <summary>
    /// 完了した承認ステップ（承認済み・却下）
    /// </summary>
    public ApprovalStep[] CompletedSteps { get; init; } = Array.Empty<ApprovalStep>();

    /// <summary>
    /// 残りの承認ステップ（未処理）
    /// </summary>
    public ApprovalStep[] RemainingSteps { get; init; } = Array.Empty<ApprovalStep>();

    /// <summary>
    /// 終端状態か（承認済み/却下/キャンセル）
    /// </summary>
    public bool IsTerminalState { get; init; }

    /// <summary>
    /// 許可されたアクション（UI側でボタン表示制御に使用）
    /// </summary>
    public string[] AllowedActions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 現在のステータス表示情報
    /// </summary>
    public StatusDisplayInfo StatusDisplay { get; init; } = null!;
}

/// <summary>
/// ステータス表示情報（UIに依存しないステータスの意味論）
/// </summary>
public record StatusDisplayInfo
{
    /// <summary>
    /// ステータスラベル（日本語）
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// バッジの色クラス（Bootstrap）
    /// </summary>
    public required string BadgeColorClass { get; init; }

    /// <summary>
    /// ステータスの重要度（Info/Warning/Success/Danger）
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// ステータスから表示情報を生成
    /// </summary>
    public static StatusDisplayInfo FromStatus(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.Draft => new StatusDisplayInfo
            {
                Label = "下書き",
                BadgeColorClass = "bg-secondary",
                Severity = "Info"
            },
            PurchaseRequestStatus.Submitted => new StatusDisplayInfo
            {
                Label = "提出済み",
                BadgeColorClass = "bg-info",
                Severity = "Info"
            },
            PurchaseRequestStatus.PendingFirstApproval or
            PurchaseRequestStatus.PendingSecondApproval or
            PurchaseRequestStatus.PendingFinalApproval => new StatusDisplayInfo
            {
                Label = GetApprovalPendingLabel(status),
                BadgeColorClass = "bg-warning",
                Severity = "Warning"
            },
            PurchaseRequestStatus.Approved => new StatusDisplayInfo
            {
                Label = "承認済み",
                BadgeColorClass = "bg-success",
                Severity = "Success"
            },
            PurchaseRequestStatus.Rejected => new StatusDisplayInfo
            {
                Label = "却下",
                BadgeColorClass = "bg-danger",
                Severity = "Danger"
            },
            PurchaseRequestStatus.Cancelled => new StatusDisplayInfo
            {
                Label = "キャンセル",
                BadgeColorClass = "bg-dark",
                Severity = "Info"
            },
            _ => new StatusDisplayInfo
            {
                Label = "不明",
                BadgeColorClass = "bg-light",
                Severity = "Info"
            }
        };
    }

    private static string GetApprovalPendingLabel(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.PendingFirstApproval => "1次承認待ち",
            PurchaseRequestStatus.PendingSecondApproval => "2次承認待ち",
            PurchaseRequestStatus.PendingFinalApproval => "3次承認待ち",
            _ => "承認待ち"
        };
    }
}
