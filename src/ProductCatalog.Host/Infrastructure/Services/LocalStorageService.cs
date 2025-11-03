using Microsoft.JSInterop;
using System.Text.Json;

namespace ProductCatalog.Web.Infrastructure.Services;

/// <summary>
/// LocalStorage アクセス用サービス
///
/// 設計方針:
/// - JSInterop経由でブラウザのLocalStorageにアクセス
/// - JSON形式でシリアライズ/デシリアライズ
/// - 非同期操作をサポート
/// </summary>
public sealed class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public LocalStorageService(
        IJSRuntime jsRuntime,
        ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// 値を保存
    /// </summary>
    public async Task SetItemAsync<T>(string key, T value, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ct, key, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageへの保存に失敗しました: Key={Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 値を取得
    /// </summary>
    public async Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ct, key);

            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageからの取得に失敗しました: Key={Key}", key);
            return default;
        }
    }

    /// <summary>
    /// 値を削除
    /// </summary>
    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ct, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageからの削除に失敗しました: Key={Key}", key);
            throw;
        }
    }

    /// <summary>
    /// すべてクリア
    /// </summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageのクリアに失敗しました");
            throw;
        }
    }

    /// <summary>
    /// キーの存在確認
    /// </summary>
    public async Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ct, key);
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageの確認に失敗しました: Key={Key}", key);
            return false;
        }
    }
}
