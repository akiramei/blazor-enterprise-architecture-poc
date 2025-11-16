using Microsoft.JSInterop;
using System.Text.Json;

namespace Application.Infrastructure.Services;

/// <summary>
/// LocalStorage 繧｢繧ｯ繧ｻ繧ｹ逕ｨ繧ｵ繝ｼ繝薙せ
///
/// 險ｭ險域婿驥・
/// - JSInterop邨檎罰縺ｧ繝悶Λ繧ｦ繧ｶ縺ｮLocalStorage縺ｫ繧｢繧ｯ繧ｻ繧ｹ
/// - JSON蠖｢蠑上〒繧ｷ繝ｪ繧｢繝ｩ繧､繧ｺ/繝・す繝ｪ繧｢繝ｩ繧､繧ｺ
/// - 髱槫酔譛滓桃菴懊ｒ繧ｵ繝昴・繝・
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
    /// 蛟､繧剃ｿ晏ｭ・
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
            _logger.LogError(ex, "LocalStorage縺ｸ縺ｮ菫晏ｭ倥↓螟ｱ謨励＠縺ｾ縺励◆: Key={Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 蛟､繧貞叙蠕・
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
            _logger.LogError(ex, "LocalStorage縺九ｉ縺ｮ蜿門ｾ励↓螟ｱ謨励＠縺ｾ縺励◆: Key={Key}", key);
            return default;
        }
    }

    /// <summary>
    /// 蛟､繧貞炎髯､
    /// </summary>
    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ct, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage縺九ｉ縺ｮ蜑企勁縺ｫ螟ｱ謨励＠縺ｾ縺励◆: Key={Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 縺吶∋縺ｦ繧ｯ繝ｪ繧｢
    /// </summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorage縺ｮ繧ｯ繝ｪ繧｢縺ｫ螟ｱ謨励＠縺ｾ縺励◆");
            throw;
        }
    }

    /// <summary>
    /// 繧ｭ繝ｼ縺ｮ蟄伜惠遒ｺ隱・
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
            _logger.LogError(ex, "LocalStorage縺ｮ遒ｺ隱阪↓螟ｱ謨励＠縺ｾ縺励◆: Key={Key}", key);
            return false;
        }
    }
}
