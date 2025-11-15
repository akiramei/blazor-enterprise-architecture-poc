namespace Domain.PurchaseManagement.PurchaseRequests.Boundaries;

/// <summary>
/// UIメタデータ：ドメイン知識をUIレンダリング情報としてプッシュ
///
/// 【パターン: UI Policy Push】
/// ドメイン層がUIの表示方法を決定し、UI層はそれに従って描画するだけ
///
/// 利点:
/// - UIロジックの一元化（switch文やif文の排除）
/// - ドメイン知識のカプセル化
/// - UI層の単純化（データバインディングのみ）
/// - テスタビリティの向上
/// </summary>
public sealed record UIMetadata
{
    /// <summary>
    /// 表示ヒント（UI層への指示）
    /// </summary>
    public required RenderingHints Rendering { get; init; }

    /// <summary>
    /// アクセシビリティ情報
    /// </summary>
    public required AccessibilityInfo Accessibility { get; init; }

    /// <summary>
    /// インタラクション情報（ボタン、リンクなど）
    /// </summary>
    public IReadOnlyList<InteractionHint> Interactions { get; init; } = Array.Empty<InteractionHint>();

    /// <summary>
    /// レイアウトヒント（表示優先度、配置など）
    /// </summary>
    public LayoutHints? Layout { get; init; }

    /// <summary>
    /// 承認ステップ用のUIメタデータを生成
    /// </summary>
    public static UIMetadata ForApprovalStep(ApprovalStepStatus status)
    {
        return status switch
        {
            ApprovalStepStatus.Pending => new UIMetadata
            {
                Rendering = new RenderingHints
                {
                    BadgeColorClass = "bg-warning text-dark",
                    BorderColorClass = "border-warning",
                    IconClass = "bi-hourglass-split",
                    EmphasisLevel = EmphasisLevel.Medium
                },
                Accessibility = new AccessibilityInfo
                {
                    AriaLabel = "承認待ち",
                    Role = "status",
                    LiveRegion = "polite"
                }
            },
            ApprovalStepStatus.Approved => new UIMetadata
            {
                Rendering = new RenderingHints
                {
                    BadgeColorClass = "bg-success",
                    BorderColorClass = "border-success",
                    IconClass = "bi-check-circle-fill",
                    EmphasisLevel = EmphasisLevel.Low
                },
                Accessibility = new AccessibilityInfo
                {
                    AriaLabel = "承認済み",
                    Role = "status",
                    LiveRegion = "off"
                }
            },
            ApprovalStepStatus.Rejected => new UIMetadata
            {
                Rendering = new RenderingHints
                {
                    BadgeColorClass = "bg-danger",
                    BorderColorClass = "border-danger",
                    IconClass = "bi-x-circle-fill",
                    EmphasisLevel = EmphasisLevel.High
                },
                Accessibility = new AccessibilityInfo
                {
                    AriaLabel = "却下",
                    Role = "alert",
                    LiveRegion = "assertive"
                }
            },
            _ => new UIMetadata
            {
                Rendering = new RenderingHints
                {
                    BadgeColorClass = "bg-secondary",
                    BorderColorClass = "border-secondary",
                    IconClass = "bi-question-circle",
                    EmphasisLevel = EmphasisLevel.Low
                },
                Accessibility = new AccessibilityInfo
                {
                    AriaLabel = "不明",
                    Role = "status",
                    LiveRegion = "off"
                }
            }
        };
    }

    /// <summary>
    /// 購買申請ステータス用のUIメタデータを生成（StatusDisplayInfoを拡張）
    /// </summary>
    public static UIMetadata ForRequestStatus(PurchaseRequestStatus status)
    {
        var displayInfo = StatusDisplayInfo.FromStatus(status);
        var emphasisLevel = displayInfo.Severity switch
        {
            "Danger" => EmphasisLevel.High,
            "Warning" => EmphasisLevel.Medium,
            "Success" => EmphasisLevel.Low,
            _ => EmphasisLevel.Low
        };

        var iconClass = status switch
        {
            PurchaseRequestStatus.Draft => "bi-file-earmark",
            PurchaseRequestStatus.Submitted => "bi-send",
            PurchaseRequestStatus.PendingFirstApproval or
            PurchaseRequestStatus.PendingSecondApproval or
            PurchaseRequestStatus.PendingFinalApproval => "bi-hourglass-split",
            PurchaseRequestStatus.Approved => "bi-check-circle-fill",
            PurchaseRequestStatus.Rejected => "bi-x-circle-fill",
            PurchaseRequestStatus.Cancelled => "bi-slash-circle",
            _ => "bi-question-circle"
        };

        return new UIMetadata
        {
            Rendering = new RenderingHints
            {
                BadgeColorClass = displayInfo.BadgeColorClass,
                BorderColorClass = $"border-{GetBootstrapColor(displayInfo.BadgeColorClass)}",
                IconClass = iconClass,
                EmphasisLevel = emphasisLevel
            },
            Accessibility = new AccessibilityInfo
            {
                AriaLabel = displayInfo.Label,
                Role = displayInfo.Severity == "Danger" ? "alert" : "status",
                LiveRegion = displayInfo.Severity == "Danger" ? "assertive" : "polite"
            }
        };
    }

    private static string GetBootstrapColor(string badgeClass)
    {
        // "bg-success" → "success"
        return badgeClass.Replace("bg-", "");
    }
}

/// <summary>
/// レンダリングヒント（CSS、アイコン、強調レベル）
/// </summary>
public sealed record RenderingHints
{
    /// <summary>Bootstrap バッジの色クラス（例: "bg-success", "bg-danger"）</summary>
    public required string BadgeColorClass { get; init; }

    /// <summary>Bootstrap ボーダーの色クラス（例: "border-success"）</summary>
    public required string BorderColorClass { get; init; }

    /// <summary>Bootstrap アイコンクラス（例: "bi-check-circle-fill"）</summary>
    public required string IconClass { get; init; }

    /// <summary>強調レベル（UI層での表示優先度）</summary>
    public EmphasisLevel EmphasisLevel { get; init; }

    /// <summary>追加のCSSクラス（カスタマイズ用）</summary>
    public string? AdditionalClasses { get; init; }
}

/// <summary>
/// 強調レベル（UIでの重要度を表現）
/// </summary>
public enum EmphasisLevel
{
    /// <summary>低（通常状態、完了済み）</summary>
    Low = 0,

    /// <summary>中（注意が必要、進行中）</summary>
    Medium = 1,

    /// <summary>高（重要、エラー、要対応）</summary>
    High = 2,

    /// <summary>緊急（即座の対応が必要）</summary>
    Critical = 3
}

/// <summary>
/// アクセシビリティ情報（WCAG準拠）
/// </summary>
public sealed record AccessibilityInfo
{
    /// <summary>ARIA ラベル</summary>
    public required string AriaLabel { get; init; }

    /// <summary>ARIA ロール（例: "status", "alert", "button"）</summary>
    public required string Role { get; init; }

    /// <summary>ARIA ライブリージョン（例: "polite", "assertive", "off"）</summary>
    public string LiveRegion { get; init; } = "off";

    /// <summary>ARIA 説明文（詳細な説明が必要な場合）</summary>
    public string? AriaDescription { get; init; }

    /// <summary>キーボードショートカット（例: "Ctrl+Enter"）</summary>
    public string? KeyboardShortcut { get; init; }
}

/// <summary>
/// インタラクションヒント（ボタン、リンクなどのUI要素）
/// </summary>
public sealed record InteractionHint
{
    /// <summary>インタラクションの種類</summary>
    public required InteractionType Type { get; init; }

    /// <summary>ラベル（表示テキスト）</summary>
    public required string Label { get; init; }

    /// <summary>アクション（実行する処理の識別子）</summary>
    public required string Action { get; init; }

    /// <summary>CSSクラス（ボタンスタイルなど）</summary>
    public string CssClass { get; init; } = "btn btn-secondary";

    /// <summary>アイコン</summary>
    public string? Icon { get; init; }

    /// <summary>有効/無効</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>無効化の理由（IsEnabled=falseの場合）</summary>
    public string? DisabledReason { get; init; }

    /// <summary>確認が必要か</summary>
    public bool RequiresConfirmation { get; init; }

    /// <summary>確認メッセージ</summary>
    public string? ConfirmationMessage { get; init; }
}

/// <summary>
/// インタラクションの種類
/// </summary>
public enum InteractionType
{
    /// <summary>プライマリボタン（主要アクション）</summary>
    PrimaryButton,

    /// <summary>セカンダリボタン（副次アクション）</summary>
    SecondaryButton,

    /// <summary>リンク</summary>
    Link,

    /// <summary>ドロップダウンメニュー</summary>
    Dropdown,

    /// <summary>コンテキストメニュー</summary>
    ContextMenu
}

/// <summary>
/// レイアウトヒント（表示位置、優先度など）
/// </summary>
public sealed record LayoutHints
{
    /// <summary>表示優先度（高い方が先に表示）</summary>
    public int Priority { get; init; }

    /// <summary>グルーピング情報（同じグループの要素をまとめて表示）</summary>
    public string? GroupKey { get; init; }

    /// <summary>位置ヒント（例: "toolbar", "sidebar", "footer"）</summary>
    public string? Placement { get; init; }

    /// <summary>レスポンシブ表示（モバイルでは非表示など）</summary>
    public ResponsiveVisibility Visibility { get; init; } = ResponsiveVisibility.Always;
}

/// <summary>
/// レスポンシブ表示の可視性
/// </summary>
public enum ResponsiveVisibility
{
    /// <summary>常に表示</summary>
    Always,

    /// <summary>デスクトップのみ</summary>
    DesktopOnly,

    /// <summary>モバイルのみ</summary>
    MobileOnly,

    /// <summary>タブレット以上</summary>
    TabletAndUp,

    /// <summary>非表示</summary>
    Hidden
}
