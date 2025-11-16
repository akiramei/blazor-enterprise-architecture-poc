namespace Application.Infrastructure.Models;

/// <summary>
/// 繝・・繝樒憾諷・- 繧｢繝励Μ繧ｱ繝ｼ繧ｷ繝ｧ繝ｳ縺ｮ繝・・繝櫁ｨｭ螳壹ｒ菫晄戟
///
/// 險ｭ險域婿驥・
/// - 繝繝ｼ繧ｯ繝｢繝ｼ繝・繝ｩ繧､繝医Δ繝ｼ繝峨・蛻・ｊ譖ｿ縺医ｒ繧ｵ繝昴・繝・
/// - LocalStorage縺ｫ豌ｸ邯壼喧
/// - 繧ｷ繧ｹ繝・Β險ｭ螳壹↓蠕薙≧繧ｪ繝励す繝ｧ繝ｳ繧呈署萓・
/// </summary>
public sealed record ThemeState
{
    /// <summary>
    /// 繝・・繝槭Δ繝ｼ繝・
    /// </summary>
    public ThemeMode Mode { get; init; }

    /// <summary>
    /// 繧ｷ繧ｹ繝・Β縺ｮ繝・・繝櫁ｨｭ螳壹↓蠕薙≧縺・
    /// </summary>
    public bool UseSystemTheme { get; init; }

    /// <summary>
    /// 蛻晄悄蛹紋ｸｭ繝輔Λ繧ｰ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// 繝・ヵ繧ｩ繝ｫ繝育憾諷具ｼ医Λ繧､繝医Δ繝ｼ繝会ｼ・
    /// </summary>
    public static ThemeState Default => new()
    {
        Mode = ThemeMode.Light,
        UseSystemTheme = true,
        IsLoading = false
    };

    /// <summary>
    /// 螳滄圀縺ｫ驕ｩ逕ｨ縺輔ｌ繧九ユ繝ｼ繝槭Δ繝ｼ繝会ｼ医す繧ｹ繝・Β險ｭ螳夊・・・・
    /// </summary>
    public ThemeMode EffectiveMode(bool systemPrefersDark) =>
        UseSystemTheme
            ? (systemPrefersDark ? ThemeMode.Dark : ThemeMode.Light)
            : Mode;
}

/// <summary>
/// 繝・・繝槭Δ繝ｼ繝・
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// 繝ｩ繧､繝医Δ繝ｼ繝・
    /// </summary>
    Light,

    /// <summary>
    /// 繝繝ｼ繧ｯ繝｢繝ｼ繝・
    /// </summary>
    Dark
}
