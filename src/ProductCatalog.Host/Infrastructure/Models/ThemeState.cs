namespace ProductCatalog.Host.Infrastructure.Models;

/// <summary>
/// テーマ状態 - アプリケーションのテーマ設定を保持
///
/// 設計方針:
/// - ダークモード/ライトモードの切り替えをサポート
/// - LocalStorageに永続化
/// - システム設定に従うオプションを提供
/// </summary>
public sealed record ThemeState
{
    /// <summary>
    /// テーマモード
    /// </summary>
    public ThemeMode Mode { get; init; }

    /// <summary>
    /// システムのテーマ設定に従うか
    /// </summary>
    public bool UseSystemTheme { get; init; }

    /// <summary>
    /// 初期化中フラグ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// デフォルト状態（ライトモード）
    /// </summary>
    public static ThemeState Default => new()
    {
        Mode = ThemeMode.Light,
        UseSystemTheme = true,
        IsLoading = false
    };

    /// <summary>
    /// 実際に適用されるテーマモード（システム設定考慮）
    /// </summary>
    public ThemeMode EffectiveMode(bool systemPrefersDark) =>
        UseSystemTheme
            ? (systemPrefersDark ? ThemeMode.Dark : ThemeMode.Light)
            : Mode;
}

/// <summary>
/// テーマモード
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// ライトモード
    /// </summary>
    Light,

    /// <summary>
    /// ダークモード
    /// </summary>
    Dark
}
