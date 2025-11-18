using System.Globalization;

namespace Application.Infrastructure.Models;

/// <summary>
/// ユーザー設定状態 - 言語・タイムゾーン等のユーザー設定を保持
///
/// 設定方針:
/// - ユーザーごとの設定を管理
/// - LocalStorageに永続化
/// - デフォルト値はシステム設定から取得
/// </summary>
public sealed record PreferencesState
{
    /// <summary>
    /// 言語・カルチャ設定（例: "ja-JP", "en-US"）
    /// </summary>
    public string Culture { get; init; }

    /// <summary>
    /// タイムゾーンID（例: "Tokyo Standard Time", "UTC"）
    /// </summary>
    public string TimeZoneId { get; init; }

    /// <summary>
    /// 日付フォーマット設定
    /// </summary>
    public string DateFormat { get; init; }

    /// <summary>
    /// 時刻フォーマット設定
    /// </summary>
    public string TimeFormat { get; init; }

    /// <summary>
    /// 1ページあたりの表示件数（デフォルト）
    /// </summary>
    public int DefaultPageSize { get; init; }

    /// <summary>
    /// 初期化中フラグ
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
    /// デフォルト状態（システムのカルチャを使用）
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
    /// CultureInfoを取得
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
    /// TimeZoneInfoを取得
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
