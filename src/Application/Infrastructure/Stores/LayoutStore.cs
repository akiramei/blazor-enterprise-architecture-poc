using Application.Infrastructure.Models;
using Application.Infrastructure.Services;

namespace Application.Infrastructure.Stores;

/// <summary>
/// 繝ｬ繧､繧｢繧ｦ繝育憾諷九・邂｡逅・
///
/// 險ｭ險域婿驥・
/// - 繧ｵ繧､繝峨ヰ繝ｼ縲√リ繝薙ご繝ｼ繧ｷ繝ｧ繝ｳ繝｡繝九Η繝ｼ遲峨・UI迥ｶ諷九ｒ邂｡逅・
/// - LocalStorage縺ｫ豌ｸ邯壼喧
/// - 繝ｬ繧ｹ繝昴Φ繧ｷ繝門ｯｾ蠢懶ｼ育判髱｢繧ｵ繧､繧ｺ縺ｫ蠢懊§縺溯・蜍戊ｪｿ謨ｴ・・
/// </summary>
public sealed class LayoutStore : IDisposable
{
    private readonly LocalStorageService _localStorage;
    private readonly ILogger<LayoutStore> _logger;

    private const string StorageKey = "layout-state";

    // 荳ｦ陦悟宛蠕｡逕ｨ
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 迥ｶ諷具ｼ井ｸ榊､会ｼ・
    private LayoutState _state = LayoutState.Default;

    public event Func<Task>? OnChangeAsync;

    public LayoutStore(
        LocalStorageService localStorage,
        ILogger<LayoutStore> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public LayoutState GetState() => _state;

    /// <summary>
    /// LocalStorage縺九ｉ迥ｶ諷九ｒ隱ｭ縺ｿ霎ｼ縺ｿ
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(_state with { IsLoading = true });

            var savedState = await _localStorage.GetItemAsync<LayoutState>(StorageKey, ct);

            if (savedState != null)
            {
                await SetStateAsync(savedState with { IsLoading = false });
            }
            else
            {
                await SetStateAsync(LayoutState.Default);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "繝ｬ繧､繧｢繧ｦ繝育憾諷九・隱ｭ縺ｿ霎ｼ縺ｿ縺ｫ螟ｱ謨励＠縺ｾ縺励◆");
            await SetStateAsync(LayoutState.Default);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繧ｵ繧､繝峨ヰ繝ｼ縺ｮ陦ｨ遉ｺ/髱櫁｡ｨ遉ｺ繧貞・繧頑崛縺・
    /// </summary>
    public async Task ToggleSidebarAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { IsSidebarOpen = !_state.IsSidebarOpen };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繧ｵ繧､繝峨ヰ繝ｼ縺ｮ繝斐Φ逡吶ａ繧貞・繧頑崛縺・
    /// </summary>
    public async Task ToggleSidebarPinAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { IsSidebarPinned = !_state.IsSidebarPinned };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繝翫ン繧ｲ繝ｼ繧ｷ繝ｧ繝ｳ繝｡繝九Η繝ｼ縺ｮ謚倥ｊ縺溘◆縺ｿ繧貞・繧頑崛縺・
    /// </summary>
    public async Task ToggleNavMenuAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { IsNavMenuCollapsed = !_state.IsNavMenuCollapsed };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 繝輔Ν繧ｹ繧ｯ繝ｪ繝ｼ繝ｳ繝｢繝ｼ繝峨ｒ蛻・ｊ譖ｿ縺・
    /// </summary>
    public async Task ToggleFullScreenAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var newState = _state with { IsFullScreen = !_state.IsFullScreen };
            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 逕ｻ髱｢繧ｵ繧､繧ｺ繧定ｨｭ螳夲ｼ医Ξ繧ｹ繝昴Φ繧ｷ繝門ｯｾ蠢懶ｼ・
    /// </summary>
    public async Task SetScreenSizeAsync(ScreenSize screenSize, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // 逕ｻ髱｢繧ｵ繧､繧ｺ縺ｫ蠢懊§縺ｦ繝・ヵ繧ｩ繝ｫ繝育憾諷九ｒ隱ｿ謨ｴ
            var newState = screenSize switch
            {
                ScreenSize.Mobile => _state with
                {
                    ScreenSize = screenSize,
                    IsSidebarOpen = false,
                    IsNavMenuCollapsed = true
                },
                ScreenSize.Tablet => _state with
                {
                    ScreenSize = screenSize,
                    IsSidebarPinned = false
                },
                ScreenSize.Desktop => _state with
                {
                    ScreenSize = screenSize
                },
                _ => _state with { ScreenSize = screenSize }
            };

            await SetStateAsync(newState);
            await _localStorage.SetItemAsync(StorageKey, newState, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 迥ｶ諷九ｒ繝ｪ繧ｻ繝・ヨ
    /// </summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(LayoutState.Default);
            await _localStorage.RemoveItemAsync(StorageKey, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(LayoutState newState)
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
