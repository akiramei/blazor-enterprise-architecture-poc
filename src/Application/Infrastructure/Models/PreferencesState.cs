using System.Globalization;

namespace Application.Infrastructure.Models;

/// <summary>
/// 繝ｦ繝ｼ繧ｶ繝ｼ險ｭ螳夂憾諷・- 險隱槭・繧ｿ繧､繝繧ｾ繝ｼ繝ｳ遲峨・繝ｦ繝ｼ繧ｶ繝ｼ險ｭ螳壹ｒ菫晄戟
///
/// 險ｭ險域婿驥・
/// - 繝ｦ繝ｼ繧ｶ繝ｼ縺斐→縺ｮ險ｭ螳壹ｒ邂｡逅・
/// - LocalStorage縺ｫ豌ｸ邯壼喧
/// - 繝・ヵ繧ｩ繝ｫ繝亥､縺ｯ繧ｷ繧ｹ繝・Β險ｭ螳壹°繧牙叙蠕・
/// </summary>
public sealed record PreferencesState
{
    /// <summary>
    /// 險隱・繧ｫ繝ｫ繝√Ε險ｭ螳夲ｼ井ｾ・ "ja-JP", "en-US"・・
    /// </summary>
    public string Culture { get; init; }

    /// <summary>
    /// 繧ｿ繧､繝繧ｾ繝ｼ繝ｳID・井ｾ・ "Tokyo Standard Time", "UTC"・・
    /// </summary>
    public string TimeZoneId { get; init; }

    /// <summary>
    /// 譌･莉倥ヵ繧ｩ繝ｼ繝槭ャ繝郁ｨｭ螳・
    /// </summary>
    public string DateFormat { get; init; }

    /// <summary>
    /// 譎ょ綾繝輔か繝ｼ繝槭ャ繝郁ｨｭ螳・
    /// </summary>
    public string TimeFormat { get; init; }

    /// <summary>
    /// 1繝壹・繧ｸ縺ゅ◆繧翫・陦ｨ遉ｺ莉ｶ謨ｰ・医ョ繝輔か繝ｫ繝茨ｼ・
    /// </summary>
    public int DefaultPageSize { get; init; }

    /// <summary>
    /// 蛻晄悄蛹紋ｸｭ繝輔Λ繧ｰ
    /// </summary>
    public bool IsLoading { get; init; }

    private PreferencesState()
    {
        Culture = "ja-JP";
        TimeZoneId = TimeZoneInfo.Local.Id;
        DateFormat = "yyyy/MM/dd";
        TimeFormat = "HH:mm:ss";
        DefaultPageSize = 20;
    }

    /// <summary>
    /// 繝・ヵ繧ｩ繝ｫ繝育憾諷具ｼ医す繧ｹ繝・Β縺ｮ繧ｫ繝ｫ繝√Ε繧剃ｽｿ逕ｨ・・
    /// </summary>
    public static PreferencesState Default => new()
    {
        Culture = CultureInfo.CurrentCulture.Name,
        TimeZoneId = TimeZoneInfo.Local.Id,
        DateFormat = "yyyy/MM/dd",
        TimeFormat = "HH:mm:ss",
        DefaultPageSize = 20,
        IsLoading = false
    };

    /// <summary>
    /// CultureInfo繧貞叙蠕・
    /// </summary>
    public CultureInfo GetCultureInfo()
    {
        try
        {
            return new CultureInfo(Culture);
        }
        catch
        {
            return CultureInfo.CurrentCulture;
        }
    }

    /// <summary>
    /// TimeZoneInfo繧貞叙蠕・
    /// </summary>
    public TimeZoneInfo GetTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }
}
