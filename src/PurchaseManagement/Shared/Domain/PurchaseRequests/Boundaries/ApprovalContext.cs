namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 承認コンテキスト：UIが"何を見せるべきか"の根拠
/// 承認画面に表示する情報の完全なスナップショット
///
/// 【リファクタリング: Type-Safe Boundary】
/// string[] AllowedActions → BoundaryDecision（型安全な判定結果）
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
    /// バウンダリー判定結果（型安全な許可/拒否情報）
    /// </summary>
    public required BoundaryDecision Decision { get; init; }

    /// <summary>
    /// 現在のステータス表示情報
    /// </summary>
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
