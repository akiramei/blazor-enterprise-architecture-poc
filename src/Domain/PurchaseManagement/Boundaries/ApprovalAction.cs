namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 承認アクションの種類
/// </summary>
public enum ApprovalActionType
{
    /// <summary>承認</summary>
    Approve,

    /// <summary>却下</summary>
    Reject,

    /// <summary>キャンセル</summary>
    Cancel
}

/// <summary>
/// 承認アクション値オブジェクト
///
/// 【パターン: Rich Domain Model with UI Metadata】
///
/// 責務:
/// - アクションの種類と実行可能性を型安全に表現
/// - UIレンダリングに必要なメタデータを提供（ラベル、色、アイコン、アクセシビリティ）
/// - ドメインルールとUI表現の一貫性を保証
///
/// 利点:
/// - string[] から型安全な値オブジェクトへ（プリミティブ型執着の解消）
/// - UIメタデータをドメインが提供（UIとドメインの結合度を適切に管理）
/// - アクセシビリティ情報を標準化（ARIA属性、キーボードショートカット）
/// </summary>
public sealed record ApprovalAction
{
    /// <summary>アクションの種類</summary>
    public ApprovalActionType Type { get; init; }

    /// <summary>アクションが実行可能か</summary>
    public bool IsEnabled { get; init; }

    /// <summary>UI表示用ラベル</summary>
    public string Label { get; init; }

    /// <summary>UI表示用アイコン（Blazor Iconなど）</summary>
    public string Icon { get; init; }

    /// <summary>UI表示用カラーテーマ（primary, danger, warningなど）</summary>
    public string ColorTheme { get; init; }

    /// <summary>確認ダイアログが必要か</summary>
    public bool RequiresConfirmation { get; init; }

    /// <summary>確認ダイアログのメッセージ</summary>
    public string? ConfirmationMessage { get; init; }

    /// <summary>アクセシビリティ: ARIA ラベル</summary>
    public string AriaLabel { get; init; }

    /// <summary>アクセシビリティ: キーボードショートカット（例: "Ctrl+Enter"）</summary>
    public string? KeyboardShortcut { get; init; }

    /// <summary>無効化されている理由（IsEnabled=falseの場合）</summary>
    public string? DisabledReason { get; init; }

    private ApprovalAction(
        ApprovalActionType type,
        bool isEnabled,
        string label,
        string icon,
        string colorTheme,
        bool requiresConfirmation,
        string? confirmationMessage,
        string ariaLabel,
        string? keyboardShortcut,
        string? disabledReason)
    {
        Type = type;
        IsEnabled = isEnabled;
        Label = label;
        Icon = icon;
        ColorTheme = colorTheme;
        RequiresConfirmation = requiresConfirmation;
        ConfirmationMessage = confirmationMessage;
        AriaLabel = ariaLabel;
        KeyboardShortcut = keyboardShortcut;
        DisabledReason = disabledReason;
    }

    /// <summary>
    /// 承認アクションを作成（有効）
    /// </summary>
    public static ApprovalAction Approve() => new(
        type: ApprovalActionType.Approve,
        isEnabled: true,
        label: "承認",
        icon: "check-circle",
        colorTheme: "primary",
        requiresConfirmation: true,
        confirmationMessage: "この申請を承認してもよろしいですか？",
        ariaLabel: "この申請を承認する",
        keyboardShortcut: "Ctrl+Enter",
        disabledReason: null
    );

    /// <summary>
    /// 却下アクションを作成（有効）
    /// </summary>
    public static ApprovalAction Reject() => new(
        type: ApprovalActionType.Reject,
        isEnabled: true,
        label: "却下",
        icon: "x-circle",
        colorTheme: "danger",
        requiresConfirmation: true,
        confirmationMessage: "この申請を却下してもよろしいですか？",
        ariaLabel: "この申請を却下する",
        keyboardShortcut: "Ctrl+Shift+R",
        disabledReason: null
    );

    /// <summary>
    /// キャンセルアクションを作成（有効）
    /// </summary>
    public static ApprovalAction Cancel() => new(
        type: ApprovalActionType.Cancel,
        isEnabled: true,
        label: "キャンセル",
        icon: "ban",
        colorTheme: "warning",
        requiresConfirmation: true,
        confirmationMessage: "この申請をキャンセルしてもよろしいですか？取り消しできません。",
        ariaLabel: "この申請をキャンセルする",
        keyboardShortcut: null,
        disabledReason: null
    );

    /// <summary>
    /// 無効なアクションを作成（理由付き）
    /// </summary>
    public static ApprovalAction Disabled(
        ApprovalActionType type,
        string reason) => new(
        type: type,
        isEnabled: false,
        label: GetDefaultLabel(type),
        icon: GetDefaultIcon(type),
        colorTheme: "secondary", // 無効時はグレーアウト
        requiresConfirmation: false,
        confirmationMessage: null,
        ariaLabel: $"{GetDefaultLabel(type)}（無効: {reason}）",
        keyboardShortcut: null,
        disabledReason: reason
    );

    private static string GetDefaultLabel(ApprovalActionType type) => type switch
    {
        ApprovalActionType.Approve => "承認",
        ApprovalActionType.Reject => "却下",
        ApprovalActionType.Cancel => "キャンセル",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static string GetDefaultIcon(ApprovalActionType type) => type switch
    {
        ApprovalActionType.Approve => "check-circle",
        ApprovalActionType.Reject => "x-circle",
        ApprovalActionType.Cancel => "ban",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
