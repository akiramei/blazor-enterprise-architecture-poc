namespace Application.Infrastructure.Models;

/// <summary>
/// 繝ｬ繧､繧｢繧ｦ繝育憾諷・- UI隕∫ｴ縺ｮ陦ｨ遉ｺ/髱櫁｡ｨ遉ｺ迥ｶ諷九ｒ菫晄戟
///
/// 險ｭ險域婿驥・
/// - 繧ｵ繧､繝峨ヰ繝ｼ縲√リ繝薙ご繝ｼ繧ｷ繝ｧ繝ｳ繝｡繝九Η繝ｼ遲峨・迥ｶ諷狗ｮ｡逅・
/// - LocalStorage縺ｫ豌ｸ邯壼喧
/// - 繝ｬ繧ｹ繝昴Φ繧ｷ繝門ｯｾ蠢懶ｼ育判髱｢繧ｵ繧､繧ｺ縺ｫ蠢懊§縺溯・蜍戊ｪｿ謨ｴ・・
/// </summary>
public sealed record LayoutState
{
    /// <summary>
    /// 繧ｵ繧､繝峨ヰ繝ｼ縺ｮ陦ｨ遉ｺ迥ｶ諷・
    /// </summary>
    public bool IsSidebarOpen { get; init; }

    /// <summary>
    /// 繧ｵ繧､繝峨ヰ繝ｼ縺ｮ蝗ｺ螳夂憾諷具ｼ医ヴ繝ｳ逡吶ａ・・
    /// </summary>
    public bool IsSidebarPinned { get; init; }

    /// <summary>
    /// 繝翫ン繧ｲ繝ｼ繧ｷ繝ｧ繝ｳ繝｡繝九Η繝ｼ縺ｮ謚倥ｊ縺溘◆縺ｿ迥ｶ諷・
    /// </summary>
    public bool IsNavMenuCollapsed { get; init; }

    /// <summary>
    /// 繝輔Ν繧ｹ繧ｯ繝ｪ繝ｼ繝ｳ繝｢繝ｼ繝・
    /// </summary>
    public bool IsFullScreen { get; init; }

    /// <summary>
    /// 迴ｾ蝨ｨ縺ｮ逕ｻ髱｢繧ｵ繧､繧ｺ
    /// </summary>
    public ScreenSize ScreenSize { get; init; }

    /// <summary>
    /// 蛻晄悄蛹紋ｸｭ繝輔Λ繧ｰ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// 繝・ヵ繧ｩ繝ｫ繝育憾諷・
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
    /// 繝｢繝舌う繝ｫ逕ｨ繝・ヵ繧ｩ繝ｫ繝育憾諷・
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
/// 逕ｻ髱｢繧ｵ繧､繧ｺ
/// </summary>
public enum ScreenSize
{
    /// <summary>
    /// 繝｢繝舌う繝ｫ・・ 768px・・
    /// </summary>
    Mobile,

    /// <summary>
    /// 繧ｿ繝悶Ξ繝・ヨ・・68px - 1024px・・
    /// </summary>
    Tablet,

    /// <summary>
    /// 繝・せ繧ｯ繝医ャ繝暦ｼ・ 1024px・・
    /// </summary>
    Desktop
}
