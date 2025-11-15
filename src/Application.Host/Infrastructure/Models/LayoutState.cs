namespace Application.Host.Infrastructure.Models;

/// <summary>
/// レイアウト状態 - UI要素の表示/非表示状態を保持
///
/// 設計方針:
/// - サイドバー、ナビゲーションメニュー等の状態管理
/// - LocalStorageに永続化
/// - レスポンシブ対応（画面サイズに応じた自動調整）
/// </summary>
public sealed record LayoutState
{
    /// <summary>
    /// サイドバーの表示状態
    /// </summary>
    public bool IsSidebarOpen { get; init; }

    /// <summary>
    /// サイドバーの固定状態（ピン留め）
    /// </summary>
    public bool IsSidebarPinned { get; init; }

    /// <summary>
    /// ナビゲーションメニューの折りたたみ状態
    /// </summary>
    public bool IsNavMenuCollapsed { get; init; }

    /// <summary>
    /// フルスクリーンモード
    /// </summary>
    public bool IsFullScreen { get; init; }

    /// <summary>
    /// 現在の画面サイズ
    /// </summary>
    public ScreenSize ScreenSize { get; init; }

    /// <summary>
    /// 初期化中フラグ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// デフォルト状態
    /// </summary>
    public static LayoutState Default => new()
    {
        IsSidebarOpen = true,
        IsSidebarPinned = true,
        IsNavMenuCollapsed = false,
        IsFullScreen = false,
        ScreenSize = ScreenSize.Desktop,
        IsLoading = false
    };

    /// <summary>
    /// モバイル用デフォルト状態
    /// </summary>
    public static LayoutState MobileDefault => new()
    {
        IsSidebarOpen = false,
        IsSidebarPinned = false,
        IsNavMenuCollapsed = true,
        IsFullScreen = false,
        ScreenSize = ScreenSize.Mobile,
        IsLoading = false
    };
}

/// <summary>
/// 画面サイズ
/// </summary>
public enum ScreenSize
{
    /// <summary>
    /// モバイル（< 768px）
    /// </summary>
    Mobile,

    /// <summary>
    /// タブレット（768px - 1024px）
    /// </summary>
    Tablet,

    /// <summary>
    /// デスクトップ（> 1024px）
    /// </summary>
    Desktop
}
