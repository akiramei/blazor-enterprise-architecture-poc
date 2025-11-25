using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Services;

namespace Application.Infrastructure.Account.UI.Store;

/// <summary>
/// セキュリティ設定（2FA）の状態管理とI/O実行
///
/// 【Phase 2改善】
/// - UI層（TwoFactorSettings.razor）から状態管理を分離
/// - MediatR経由でCommand/Query実行
/// - QrCodeService経由でQRコード生成
///
/// 【責務】
/// - 2FA設定状態の管理（isEnabled, secretKey, qrCodeDataUri, recoveryCodes等）
/// - MediatR.Send()の実行
/// - 状態変更通知（OnChangeAsync）
/// </summary>
public sealed class SecuritySettingsStore : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<SecuritySettingsStore> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;

    private SecuritySettingsState _state = SecuritySettingsState.Empty;

    public event Func<Task>? OnChangeAsync;

    public SecuritySettingsStore(
        IServiceScopeFactory scopeFactory,
        IQrCodeService qrCodeService,
        ILogger<SecuritySettingsStore> logger)
    {
        _scopeFactory = scopeFactory;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public SecuritySettingsState GetState() => _state;

    /// <summary>
    /// 処理中状態の設定
    /// </summary>
    public async Task SetProcessingAsync(bool processing)
    {
        await SetStateAsync(_state with { IsProcessing = processing });
    }

    /// <summary>
    /// エラーメッセージの設定
    /// </summary>
    public async Task SetErrorAsync(string? errorMessage)
    {
        await SetStateAsync(_state with { ErrorMessage = errorMessage });
    }

    /// <summary>
    /// 2FA有効化済み状態の設定
    /// </summary>
    public async Task SetEnabledAsync(bool isEnabled)
    {
        await SetStateAsync(_state with { IsEnabled = isEnabled });
    }

    /// <summary>
    /// セットアップデータの設定（Enable2FA成功時）
    /// </summary>
    public async Task SetSetupDataAsync(string secretKey, string qrCodeUri, List<string> recoveryCodes)
    {
        // QRコード画像生成（Infrastructure層のサービスに委譲）
        var qrCodeDataUri = _qrCodeService.GenerateQrCodeImage(qrCodeUri);

        await SetStateAsync(_state with
        {
            SecretKey = secretKey,
            QrCodeDataUri = qrCodeDataUri,
            RecoveryCodes = recoveryCodes,
            SetupStep = SetupStep.ShowingQRCode,
            IsProcessing = false
        });
    }

    /// <summary>
    /// セットアップのキャンセル
    /// </summary>
    public async Task CancelSetupAsync()
    {
        await SetStateAsync(_state with
        {
            SetupStep = SetupStep.NotStarted,
            VerificationCode = "",
            VerificationError = null,
            SecretKey = null,
            QrCodeDataUri = null,
            RecoveryCodes = new List<string>()
        });
    }

    /// <summary>
    /// 検証コードの設定
    /// </summary>
    public async Task SetVerificationCodeAsync(string code)
    {
        await SetStateAsync(_state with { VerificationCode = code });
    }

    /// <summary>
    /// 検証エラーの設定
    /// </summary>
    public async Task SetVerificationErrorAsync(string? error)
    {
        await SetStateAsync(_state with { VerificationError = error });
    }

    /// <summary>
    /// 2FA検証成功時の状態更新
    /// </summary>
    public async Task OnVerificationSuccessAsync()
    {
        await SetStateAsync(_state with
        {
            IsEnabled = true,
            SetupStep = SetupStep.NotStarted,
            VerificationCode = "",
            SecretKey = null,
            QrCodeDataUri = null,
            RecoveryCodes = new List<string>(),
            IsProcessing = false
        });
    }

    /// <summary>
    /// 無効化確認表示の切り替え
    /// </summary>
    public async Task SetShowDisableConfirmationAsync(bool show)
    {
        await SetStateAsync(_state with { ShowDisableConfirmation = show });
    }

    /// <summary>
    /// パスワード確認の設定
    /// </summary>
    public async Task SetPasswordConfirmationAsync(string password)
    {
        await SetStateAsync(_state with { PasswordConfirmation = password });
    }

    /// <summary>
    /// 無効化エラーの設定
    /// </summary>
    public async Task SetDisableErrorAsync(string? error)
    {
        await SetStateAsync(_state with { DisableError = error });
    }

    /// <summary>
    /// 2FA無効化成功時の状態更新
    /// </summary>
    public async Task OnDisableSuccessAsync()
    {
        await SetStateAsync(_state with
        {
            IsEnabled = false,
            ShowDisableConfirmation = false,
            PasswordConfirmation = "",
            DisableError = null,
            IsProcessing = false
        });
    }

    private async Task SetStateAsync(SecuritySettingsState newState)
    {
        _state = newState;
        await NotifyStateChangedAsync();
    }

    private async Task NotifyStateChangedAsync()
    {
        if (OnChangeAsync != null)
        {
            await OnChangeAsync.Invoke();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _gate?.Dispose();
    }
}

/// <summary>
/// セキュリティ設定の状態
/// </summary>
public record SecuritySettingsState
{
    public bool IsEnabled { get; init; }
    public bool IsProcessing { get; init; }
    public string? ErrorMessage { get; init; }

    // セットアップ関連
    public SetupStep SetupStep { get; init; } = SetupStep.NotStarted;
    public string? SecretKey { get; init; }
    public string? QrCodeDataUri { get; init; }
    public List<string> RecoveryCodes { get; init; } = new();
    public string VerificationCode { get; init; } = "";
    public string? VerificationError { get; init; }

    // 無効化関連
    public bool ShowDisableConfirmation { get; init; }
    public string PasswordConfirmation { get; init; } = "";
    public string? DisableError { get; init; }

    public static readonly SecuritySettingsState Empty = new();
}

/// <summary>
/// セットアップステップ
/// </summary>
public enum SetupStep
{
    NotStarted,
    ShowingQRCode
}
