namespace Shared.Application.Interfaces;

/// <summary>
/// キャッシュ可能なQueryを示すマーカーインターフェース
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// キャッシュキーを取得（ユーザー/テナント情報はBehaviorが自動付与）
    /// </summary>
    string GetCacheKey();

    /// <summary>
    /// キャッシュ期間（分）
    /// </summary>
    int CacheDurationMinutes { get; }
}
