using Application.Host.Infrastructure.Models;
using Application.Host.Infrastructure.Services;

namespace Application.Host.Infrastructure.Stores;

/// <summary>
/// レイアウト状態の管理
///
/// 設計方針:
/// - サイドバー、ナビゲーションメニュー等のUI状態を管理
/// - LocalStorageに永続化
/// - レスポンシブ対応（画面サイズに応じた自動調整）
/// </summary>
public sealed class LayoutStore : IDisposable
{
    private readonly LocalStorageService _localStorage;
    private readonly ILogger<LayoutStore> _logger;

    private const string StorageKey = "layout-state";

    // 並行制御用
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 状態（不変）
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
    /// LocalStorageから状態を読み込み
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
            _logger.LogError(ex, "レイアウト状態の読み込みに失敗しました");
            await SetStateAsync(LayoutState.Default);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// サイドバーの表示/非表示を切り替え
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
    /// サイドバーのピン留めを切り替え
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
    /// ナビゲーションメニューの折りたたみを切り替え
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
    /// フルスクリーンモードを切り替え
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
    /// 画面サイズを設定（レスポンシブ対応）
    /// </summary>
    public async Task SetScreenSizeAsync(ScreenSize screenSize, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // 画面サイズに応じてデフォルト状態を調整
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
    /// 状態をリセット
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
                _logger.LogError(ex, "状態変更通知中にエラーが発生しました");
            }
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
