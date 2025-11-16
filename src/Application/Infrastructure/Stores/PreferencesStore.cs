using Application.Infrastructure.Models;
using Application.Infrastructure.Services;

namespace Application.Infrastructure.Stores;

/// <summary>
/// 繝ｦ繝ｼ繧ｶ繝ｼ險ｭ螳壹・迥ｶ諷狗ｮ｡逅・
///
/// 險ｭ險域婿驥・
/// - 險隱槭・繧ｿ繧､繝繧ｾ繝ｼ繝ｳ繝ｻ譌･莉倥ヵ繧ｩ繝ｼ繝槭ャ繝育ｭ峨・繝ｦ繝ｼ繧ｶ繝ｼ險ｭ螳壹ｒ邂｡逅・
/// - LocalStorage縺ｫ豌ｸ邯壼喧
/// - 荳ｦ陦悟宛蠕｡縺ｫ繧医ｋ螳牙・縺ｪ迥ｶ諷区峩譁ｰ
/// </summary>
public sealed class PreferencesStore : IDisposable
{
    private readonly LocalStorageService _localStorage;
    private readonly ILogger<PreferencesStore> _logger;

    private const string StorageKey = "user-preferences";

    // 荳ｦ陦悟宛蠕｡逕ｨ
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 迥ｶ諷具ｼ井ｸ榊､会ｼ・
    private PreferencesState _state = PreferencesState.Default;

    public event Func<Task>? OnChangeAsync;

    public PreferencesStore(
        LocalStorageService localStorage,
        ILogger<PreferencesStore> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public PreferencesState GetState() => _state;

    /// <summary>
    /// LocalStorage縺九ｉ險ｭ螳壹ｒ隱ｭ縺ｿ霎ｼ縺ｿ
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(_state with { IsLoading = true });

            var savedState = await _localStorage.GetItemAsync<PreferencesState>(StorageKey, ct);

            if (savedState != null)
            {
                await SetStateAsync(savedState with { IsLoading = false });
            }
            else
            {
                await SetStateAsync(PreferencesState.Default);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "險ｭ螳壹・隱ｭ縺ｿ霎ｼ縺ｿ縺ｫ螟ｱ謨励＠縺ｾ縺励◆");
            await SetStateAsync(PreferencesState.Default);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繧ｫ繝ｫ繝√Ε繧定ｨｭ螳・
    /// </summary>
    public async Task SetCultureAsync(string culture, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { Culture = culture };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繧ｿ繧､繝繧ｾ繝ｼ繝ｳ繧定ｨｭ螳・
    /// </summary>
    public async Task SetTimeZoneAsync(string timeZoneId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { TimeZoneId = timeZoneId };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 譌･莉倥ヵ繧ｩ繝ｼ繝槭ャ繝医ｒ險ｭ螳・
    /// </summary>
    public async Task SetDateFormatAsync(string dateFormat, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { DateFormat = dateFormat };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 譎ょ綾繝輔か繝ｼ繝槭ャ繝医ｒ險ｭ螳・
    /// </summary>
    public async Task SetTimeFormatAsync(string timeFormat, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { TimeFormat = timeFormat };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繝・ヵ繧ｩ繝ｫ繝医・繝ｼ繧ｸ繧ｵ繧､繧ｺ繧定ｨｭ螳・
    /// </summary>
    public async Task SetDefaultPageSizeAsync(int pageSize, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { DefaultPageSize = pageSize };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 險ｭ螳壹ｒ繝ｪ繧ｻ繝・ヨ
    /// </summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(PreferencesState.Default);
            await _localStorage.RemoveItemAsync(StorageKey, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(PreferencesState newState)
    {
        _state = newState;

        if (OnChangeAsync is null) return;

        foreach (var handler in OnChangeAsync.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迥ｶ諷句､画峩騾夂衍荳ｭ縺ｫ繧ｨ繝ｩ繝ｼ縺檎匱逕溘＠縺ｾ縺励◆");
            }
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
