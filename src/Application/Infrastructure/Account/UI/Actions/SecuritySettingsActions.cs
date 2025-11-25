using Application.Features.Disable2FA;
using Application.Features.Enable2FA;
using Application.Features.Verify2FA;
using Application.Infrastructure.Account.UI.Store;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Infrastructure.Account.UI.Actions;

/// <summary>
/// セキュリティ設定（2FA）画面のUI手順を管理
///
/// 【Phase 2改善】
/// - UI層（TwoFactorSettings.razor）からビジネスロジックを完全分離
/// - MediatR.Send()の実行をActionsに集約
/// - エラーハンドリングとログ出力を一元化
///
/// 【責務】
/// - Command/Queryの実行
/// - Store経由での状態更新
/// - エラーハンドリング
/// </summary>
public sealed class SecuritySettingsActions
{
    private readonly SecuritySettingsStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecuritySettingsActions> _logger;

    public SecuritySettingsActions(
        SecuritySettingsStore store,
        IServiceScopeFactory scopeFactory,
        ILogger<SecuritySettingsActions> logger)
    {
        _store = store;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 2FA有効化開始
    /// </summary>
    public async Task Enable2FAAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("[Actions] Enable2FA started for UserId: {UserId}", userId);

        await _store.SetProcessingAsync(true);
        await _store.SetErrorAsync(null);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new Enable2FACommand { UserId = userId };
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Actions] Enable2FA succeeded");
                await _store.SetSetupDataAsync(
                    result.Value.SecretKey,
                    result.Value.QrCodeUri,
                    result.Value.RecoveryCodes);
            }
            else
            {
                _logger.LogWarning("[Actions] Enable2FA failed: {Error}", result.Error);
                await _store.SetErrorAsync(result.Error);
                await _store.SetProcessingAsync(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Actions] 2FA有効化開始に失敗しました");
            await _store.SetErrorAsync("エラーが発生しました。もう一度お試しください。");
            await _store.SetProcessingAsync(false);
        }
    }

    /// <summary>
    /// 2FA検証・確定
    /// </summary>
    public async Task Verify2FAAsync(Guid userId, string verificationCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[Actions] Verify2FA started for UserId: {UserId}", userId);

        await _store.SetProcessingAsync(true);
        await _store.SetVerificationErrorAsync(null);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new Verify2FACommand
            {
                UserId = userId,
                VerificationCode = verificationCode
            };
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Actions] Verify2FA succeeded");
                await _store.OnVerificationSuccessAsync();
            }
            else
            {
                _logger.LogWarning("[Actions] Verify2FA failed: {Error}", result.Error);
                await _store.SetVerificationErrorAsync(result.Error);
                await _store.SetProcessingAsync(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Actions] 2FA検証に失敗しました");
            await _store.SetVerificationErrorAsync("エラーが発生しました。もう一度お試しください。");
            await _store.SetProcessingAsync(false);
        }
    }

    /// <summary>
    /// 2FA無効化
    /// </summary>
    public async Task Disable2FAAsync(Guid userId, string password, CancellationToken ct = default)
    {
        _logger.LogInformation("[Actions] Disable2FA started for UserId: {UserId}", userId);

        await _store.SetProcessingAsync(true);
        await _store.SetDisableErrorAsync(null);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new Disable2FACommand
            {
                UserId = userId,
                Password = password
            };
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Actions] Disable2FA succeeded");
                await _store.OnDisableSuccessAsync();
            }
            else
            {
                _logger.LogWarning("[Actions] Disable2FA failed: {Error}", result.Error);
                await _store.SetDisableErrorAsync(result.Error);
                await _store.SetProcessingAsync(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Actions] 2FA無効化に失敗しました");
            await _store.SetDisableErrorAsync("エラーが発生しました。もう一度お試しください。");
            await _store.SetProcessingAsync(false);
        }
    }

    /// <summary>
    /// セットアップのキャンセル
    /// </summary>
    public async Task CancelSetupAsync()
    {
        _logger.LogInformation("[Actions] CancelSetup");
        await _store.CancelSetupAsync();
    }

    /// <summary>
    /// 無効化確認の表示/非表示
    /// </summary>
    public async Task ToggleDisableConfirmationAsync(bool show)
    {
        _logger.LogInformation("[Actions] ToggleDisableConfirmation: {Show}", show);
        await _store.SetShowDisableConfirmationAsync(show);
        if (!show)
        {
            await _store.SetPasswordConfirmationAsync("");
            await _store.SetDisableErrorAsync(null);
        }
    }
}
