using System.Collections.Immutable;

namespace Application.Infrastructure.Models;

/// <summary>
/// 通知状態 - トースト通知・モーダル等の表示状態を保持
///
/// 設計方針:
/// - トースト通知のキュー管理
/// - モーダルダイアログの状態管理
/// - 自動消去機能（トースト）
/// </summary>
public sealed record NotificationState
{
    /// <summary>
    /// 表示中のトースト通知リスト
    /// </summary>
    public ImmutableList<ToastNotification> Toasts { get; init; } = ImmutableList<ToastNotification>.Empty;

    /// <summary>
    /// 現在表示中のモーダル
    /// </summary>
    public ModalNotification? CurrentModal { get; init; }

    /// <summary>
    /// 初期化中フラグ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// 空の状態
    /// </summary>
    public static NotificationState Empty => new()
    {
        Toasts = ImmutableList<ToastNotification>.Empty,
        CurrentModal = null,
        IsLoading = false
    };
}

/// <summary>
/// トースト通知
/// </summary>
public sealed record ToastNotification
{
    /// <summary>
    /// 通知ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// タイトル
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// メッセージ
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 通知種別
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Info;

    /// <summary>
    /// 表示継続時間（ミリ秒）
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 自動消去するか
    /// </summary>
    public bool AutoDismiss { get; init; } = true;
}

/// <summary>
/// モーダル通知
/// </summary>
public sealed record ModalNotification
{
    /// <summary>
    /// モーダルID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// タイトル
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// メッセージ
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 通知種別
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Info;

    /// <summary>
    /// モーダル種別
    /// </summary>
    public ModalType ModalType { get; init; } = ModalType.Alert;

    /// <summary>
    /// 確認ボタンテキスト
    /// </summary>
    public string ConfirmButtonText { get; init; } = "OK";

    /// <summary>
    /// キャンセルボタンテキスト
    /// </summary>
    public string? CancelButtonText { get; init; }

    /// <summary>
    /// 確認時のコールバック
    /// </summary>
    public Func<Task>? OnConfirm { get; init; }

    /// <summary>
    /// キャンセル時のコールバック
    /// </summary>
    public Func<Task>? OnCancel { get; init; }
}

/// <summary>
/// 通知種別
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 情報
    /// </summary>
    Info,

    /// <summary>
    /// 成功
    /// </summary>
    Success,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// エラー
    /// </summary>
    Error
}

/// <summary>
/// モーダル種別
/// </summary>
public enum ModalType
{
    /// <summary>
    /// アラート（OKのみ）
    /// </summary>
    Alert,

    /// <summary>
    /// 確認ダイアログ（OK/キャンセル）
    /// </summary>
    Confirm,

    /// <summary>
    /// カスタムコンテンツ
    /// </summary>
    Custom
}
