using System.Collections.Immutable;
using Application.Infrastructure.Models;

namespace Application.Infrastructure.Stores;

/// <summary>
/// 通知管理
///
/// 設計方針:
/// - トースト通知のキュー管理
/// - モーダルダイアログの状態管理
/// - 自動消去タイマー（トースト）
/// - LocalStorageへの永続化は不要（揮発性データ）
/// </summary>
public sealed class NotificationStore : IDisposable
{
    private readonly ILogger<NotificationStore> _logger;

    // 並行制御用
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 自動消去タイマー管理
    private readonly Dictionary<Guid, CancellationTokenSource> _toastTimers = new();

    // 状態（不変）
    private NotificationState _state = NotificationState.Empty;

    public event Func<Task>? OnChangeAsync;

    public NotificationStore(ILogger<NotificationStore> logger)
    {
        _logger = logger;
    }

    public NotificationState GetState() => _state;

    /// <summary>
    /// トースト通知を表示
    /// </summary>
    public async Task ShowToastAsync(
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        int durationMs = 5000,
        bool autoDismiss = true,
        CancellationToken ct = default)
    {
        var toast = new ToastNotification
        {
            Title = title,
            Message = message,
            Type = type,
            DurationMs = durationMs,
            AutoDismiss = autoDismiss
        };

        await _gate.WaitAsync(ct);
        try
        {
            var newToasts = _state.Toasts.Add(toast);
            await SetStateAsync(_state with { Toasts = newToasts });

            // 自動消去タイマーを設定
            if (autoDismiss)
            {
                var cts = new CancellationTokenSource();
                _toastTimers[toast.Id] = cts;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // タイマー待機
                        try
                        {
                            await Task.Delay(durationMs, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // タイマーがキャンセルされた（手動で消去された）
                            return;
                        }

                        // タイマー完了後、トーストを自動消去
                        try
                        {
                            await DismissToastAsync(toast.Id);
                        }
                        catch (Exception dismissEx)
                        {
                            _logger.LogError(dismissEx, "トースト自動消去処理中にエラーが発生しました。ToastId: {ToastId}", toast.Id);

                            // DismissToastAsyncが失敗した場合、タイマーエントリを手動でクリーンアップ
                            try
                            {
                                await _gate.WaitAsync();
                                try
                                {
                                    if (_toastTimers.TryGetValue(toast.Id, out var timerCts))
                                    {
                                        timerCts.Dispose();
                                        _toastTimers.Remove(toast.Id);
                                    }
                                }
                                finally
                                {
                                    _gate.Release();
                                }
                            }
                            catch (Exception cleanupEx)
                            {
                                _logger.LogError(cleanupEx, "タイマークリーンアップ中にエラーが発生しました。ToastId: {ToastId}", toast.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "トースト自動消去タイマーで予期しないエラーが発生しました。ToastId: {ToastId}", toast.Id);

                        // 予期しないエラーの場合もタイマーエントリをクリーンアップして、リソースリークを防ぐ
                        try
                        {
                            await _gate.WaitAsync();
                            try
                            {
                                if (_toastTimers.TryGetValue(toast.Id, out var timerCts))
                                {
                                    timerCts.Dispose();
                                    _toastTimers.Remove(toast.Id);
                                }
                            }
                            finally
                            {
                                _gate.Release();
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogError(cleanupEx, "予期しないエラー後のタイマークリーンアップ中にエラーが発生しました。ToastId: {ToastId}", toast.Id);
                        }
                    }
                });
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 成功トースト（ショートカット）
    /// </summary>
    public Task ShowSuccessAsync(string title, string message, CancellationToken ct = default)
        => ShowToastAsync(title, message, NotificationType.Success, ct: ct);

    /// <summary>
    /// エラートースト（ショートカット）
    /// </summary>
    public Task ShowErrorAsync(string title, string message, CancellationToken ct = default)
        => ShowToastAsync(title, message, NotificationType.Error, durationMs: 10000, ct: ct);

    /// <summary>
    /// 警告トースト（ショートカット）
    /// </summary>
    public Task ShowWarningAsync(string title, string message, CancellationToken ct = default)
        => ShowToastAsync(title, message, NotificationType.Warning, durationMs: 7000, ct: ct);

    /// <summary>
    /// 情報トースト（ショートカット）
    /// </summary>
    public Task ShowInfoAsync(string title, string message, CancellationToken ct = default)
        => ShowToastAsync(title, message, NotificationType.Info, ct: ct);

    /// <summary>
    /// トースト通知を消去
    /// </summary>
    public async Task DismissToastAsync(Guid toastId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // タイマーをキャンセル
            if (_toastTimers.TryGetValue(toastId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _toastTimers.Remove(toastId);
            }

            var newToasts = _state.Toasts.RemoveAll(t => t.Id == toastId);
            await SetStateAsync(_state with { Toasts = newToasts });
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// すべてのトースト通知をクリア
    /// </summary>
    public async Task ClearAllToastsAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // すべてのタイマーをキャンセル
            foreach (var cts in _toastTimers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _toastTimers.Clear();

            await SetStateAsync(_state with { Toasts = ImmutableList<ToastNotification>.Empty });
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// モーダルを表示
    /// </summary>
    public async Task ShowModalAsync(
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        ModalType modalType = ModalType.Alert,
        string confirmButtonText = "OK",
        string? cancelButtonText = null,
        Func<Task>? onConfirm = null,
        Func<Task>? onCancel = null,
        CancellationToken ct = default)
    {
        var modal = new ModalNotification
        {
            Title = title,
            Message = message,
            Type = type,
            ModalType = modalType,
            ConfirmButtonText = confirmButtonText,
            CancelButtonText = cancelButtonText,
            OnConfirm = onConfirm,
            OnCancel = onCancel
        };

        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(_state with { CurrentModal = modal });
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 確認ダイアログを表示（ショートカット）
    /// </summary>
    public Task ShowConfirmAsync(
        string title,
        string message,
        Func<Task> onConfirm,
        Func<Task>? onCancel = null,
        CancellationToken ct = default)
        => ShowModalAsync(
            title,
            message,
            NotificationType.Warning,
            ModalType.Confirm,
            "はい",
            "いいえ",
            onConfirm,
            onCancel,
            ct);

    /// <summary>
    /// モーダルを閉じる
    /// </summary>
    public async Task CloseModalAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await SetStateAsync(_state with { CurrentModal = null });
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// モーダルの確認ボタンを実行
    /// </summary>
    public async Task ConfirmModalAsync(CancellationToken ct = default)
    {
        var modal = _state.CurrentModal;
        if (modal == null) return;

        await _gate.WaitAsync(ct);
        try
        {
            if (modal.OnConfirm != null)
            {
                await modal.OnConfirm();
            }

            await SetStateAsync(_state with { CurrentModal = null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "モーダル確認処理中にエラーが発生しました");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// モーダルのキャンセルボタンを実行
    /// </summary>
    public async Task CancelModalAsync(CancellationToken ct = default)
    {
        var modal = _state.CurrentModal;
        if (modal == null) return;

        await _gate.WaitAsync(ct);
        try
        {
            if (modal.OnCancel != null)
            {
                await modal.OnCancel();
            }

            await SetStateAsync(_state with { CurrentModal = null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "モーダルキャンセル処理中にエラーが発生しました");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(NotificationState newState)
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
        // すべてのタイマーをクリーンアップ
        foreach (var cts in _toastTimers.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _toastTimers.Clear();

        _gate.Dispose();
    }
}
