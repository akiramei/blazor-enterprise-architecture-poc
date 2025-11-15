using Application.Host.Infrastructure.Models;
using Application.Host.Infrastructure.Services;

namespace Application.Host.Infrastructure.Stores;

/// <summary>
/// ユーザー設定の状態管理
///
/// 設計方針:
/// - 言語・タイムゾーン・日付フォーマット等のユーザー設定を管理
/// - LocalStorageに永続化
/// - 並行制御による安全な状態更新
/// </summary>
public sealed class PreferencesStore : IDisposable
{
    private readonly LocalStorageService _localStorage;
    private readonly ILogger<PreferencesStore> _logger;

    private const string StorageKey = "user-preferences";

    // 並行制御用
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 状態（不変）
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
    /// LocalStorageから設定を読み込み
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
            _logger.LogError(ex, "設定の読み込みに失敗しました");
            await SetStateAsync(PreferencesState.Default);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// カルチャを設定
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
    /// タイムゾーンを設定
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
    /// 日付フォーマットを設定
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
    /// 時刻フォーマットを設定
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
    /// デフォルトページサイズを設定
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
    /// 設定をリセット
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
                _logger.LogError(ex, "状態変更通知中にエラーが発生しました");
            }
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
