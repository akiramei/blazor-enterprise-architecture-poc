using Application.Core.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Shared.Application;
using Shared.Domain.Identity;
using Shared.Infrastructure.Authentication;

namespace Application.Features.Verify2FA;

/// <summary>
/// 二要素認証（2FA）検証・確定コマンドハンドラー
///
/// 【責務】
/// 1. ユーザーの秘密鍵を取得
/// 2. TOTPコードを検証（ITotpService使用）
/// 3. 検証成功時、IsTwoFactorEnabled = true に更新
/// 4. TwoFactorEnabledAtにタイムスタンプを記録
///
/// 【設計】
/// - Enable2FACommandとVerify2FACommandを分離することで、
///   セットアップの中断・再開が可能
/// - 秘密鍵はEnable2FA時にDB保存されているため、
///   このHandlerでは検証のみに集中
/// </summary>
public sealed class Verify2FACommandHandler : CommandPipeline<Verify2FACommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITotpService _totpService;

    public Verify2FACommandHandler(
        UserManager<ApplicationUser> userManager,
        ITotpService totpService)
    {
        _userManager = userManager;
        _totpService = totpService;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        Verify2FACommand cmd,
        CancellationToken ct)
    {
        // 1. ユーザー取得
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user is null)
            return Result.Fail<Unit>("ユーザーが見つかりません");

        // 2. セットアップ状態チェック
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Result.Fail<Unit>("2FAのセットアップが開始されていません。先にEnable2FAを実行してください。");

        // 3. 既に有効化済みチェック
        if (user.IsTwoFactorEnabled)
            return Result.Fail<Unit>("二要素認証は既に有効化されています");

        // 4. TOTPコード検証（ドメインロジック）
        if (!_totpService.ValidateCode(user.TwoFactorSecretKey, cmd.VerificationCode))
            return Result.Fail<Unit>("認証コードが正しくありません。もう一度お試しください。");

        // 5. 2FA有効化
        user.IsTwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Fail<Unit>($"2FAの有効化に失敗しました: {errors}");
        }

        return Result.Success(Unit.Value);
    }
}
