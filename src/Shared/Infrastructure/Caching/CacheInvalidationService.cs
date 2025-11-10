using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Caching;

/// <summary>
/// キャッシュ無効化サービスの実装
///
/// 【設計】
/// - IMemoryCacheのリフレクションを使用してパターンマッチング
/// - すべてのテナント/ユーザーのキャッシュを無効化
/// - 構造化ログでトレーサビリティを確保
///
/// 【制限事項】
/// - IMemoryCacheのプライベートフィールドにアクセスするため、
///   実装の変更に脆弱（.NET 9以降で動作しない可能性）
/// - 将来的にはRedisなど外部キャッシュへの移行を検討
///
/// 【パフォーマンス】
/// - パターンマッチングは全キーをスキャンするため、O(N)
/// - キャッシュエントリが多い場合は遅延の可能性
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IMemoryCache cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void InvalidateProduct(Guid productId)
    {
        // GetProductByIdQuery のキャッシュを無効化
        var pattern = $"GetProductByIdQuery:*:*:product_{productId}";
        InvalidateByPattern(pattern);

        _logger.LogDebug("商品キャッシュ無効化: ProductId={ProductId}", productId);
    }

    public void InvalidateProductList()
    {
        // GetProductsQuery のキャッシュを無効化
        var pattern = "GetProductsQuery:*";
        InvalidateByPattern(pattern);

        _logger.LogDebug("商品一覧キャッシュ無効化");
    }

    public void InvalidateByPattern(string pattern)
    {
        var removedCount = 0;

        try
        {
            // デバッグ: IMemoryCacheの実装型を確認
            _logger.LogDebug("IMemoryCache implementation type: {Type}", _cache.GetType().FullName);

            // IMemoryCacheの内部実装にアクセス（リフレクション）
            // Note: MemoryCacheの内部実装は2つのパターンがある:
            // 1. EntriesCollection (古い実装)
            // 2. _coherentState._entries (新しい実装、.NET 9以降)
            var cacheEntriesProperty = _cache.GetType().GetProperty("EntriesCollection",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (cacheEntriesProperty == null)
            {
                _logger.LogDebug("EntriesCollection not found, trying .NET 9 implementation");

                // .NET 9の新しい実装を試す
                var coherentStateField = _cache.GetType().GetField("_coherentState",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (coherentStateField != null)
                {
                    _logger.LogDebug("Found _coherentState field");
                    var coherentState = coherentStateField.GetValue(_cache);
                    if (coherentState != null)
                    {
                        // デバッグ: coherentStateの全フィールドを表示
                        var allFields = coherentState.GetType()
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                            .Select(f => f.Name);
                        _logger.LogDebug("coherentState fields: {Fields}", string.Join(", ", allFields));

                        var entriesField = coherentState.GetType().GetField("_entries",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (entriesField != null)
                        {
                            _logger.LogDebug("Found _entries field, using .NET 9 implementation");
                            InvalidateByPatternFromNewImplementation(entriesField, coherentState, pattern);
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("_entries field not found in coherentState");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("coherentState is null");
                    }
                }
                else
                {
                    _logger.LogWarning("_coherentState field not found");
                }

                _logger.LogWarning("キャッシュエントリの取得に失敗（IMemoryCacheの実装変更の可能性）");
                return;
            }

            var cacheEntries = cacheEntriesProperty.GetValue(_cache) as
                System.Collections.IDictionary;

            if (cacheEntries == null) return;

            var keysToRemove = new List<object>();

            // パターンに一致するキーを収集
            foreach (System.Collections.DictionaryEntry entry in cacheEntries)
            {
                var key = entry.Key?.ToString();
                if (key == null) continue;

                if (IsPatternMatch(key, pattern))
                {
                    keysToRemove.Add(entry.Key);
                }
            }

            // キャッシュから削除
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                removedCount++;
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("【キャッシュ無効化】 Pattern={Pattern}, 削除数={Count}",
                    pattern, removedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "キャッシュ無効化エラー: Pattern={Pattern}", pattern);
        }
    }

    /// <summary>
    /// .NET 9の新しいMemoryCache実装からパターンマッチングで削除
    /// </summary>
    private void InvalidateByPatternFromNewImplementation(
        System.Reflection.FieldInfo entriesField,
        object coherentState,
        string pattern)
    {
        var removedCount = 0;

        var entries = entriesField.GetValue(coherentState) as System.Collections.IDictionary;
        if (entries == null) return;

        var keysToRemove = new List<object>();

        // パターンに一致するキーを収集
        foreach (System.Collections.DictionaryEntry entry in entries)
        {
            var key = entry.Key?.ToString();
            if (key == null) continue;

            if (IsPatternMatch(key, pattern))
            {
                keysToRemove.Add(entry.Key);
            }
        }

        // キャッシュから削除
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            removedCount++;
        }

        if (removedCount > 0)
        {
            _logger.LogDebug("【キャッシュ無効化】 Pattern={Pattern}, 削除数={Count}",
                pattern, removedCount);
        }
    }

    /// <summary>
    /// 簡易的なパターンマッチング（ワイルドカード * のみサポート）
    /// </summary>
    private static bool IsPatternMatch(string input, string pattern)
    {
        // *を正規表現の.*に変換
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern);
    }
}
