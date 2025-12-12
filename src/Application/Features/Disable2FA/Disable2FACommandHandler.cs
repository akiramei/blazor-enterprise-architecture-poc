using Application.Core.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Application;
using Shared.Domain.Identity;
using Shared.Infrastructure.Platform.Persistence;

namespace Application.Features.Disable2FA;

/// <summary>
/// 二要素認証（2FA）無効化コマンドハンドラー
///
/// 【責務】
/// 1. パスワード検証（セキュリティレベルを下げる操作のため必須）
/// 2. 2FA無効化フラグの更新
/// 3. 秘密鍵の削除
/// 4. リカバリーコードの全削除
///
/// 【トランザクション】
/// GenericTransactionBehaviorにより、以下が自動的にトランザクション内で実行される：
/// - UserManager.UpdateAsync()
/// - DbContext.SaveChangesAsync()
/// 片方が失敗した場合、全体がロールバックされる。
/// </summary>
public class Disable2FACommandHandler : CommandPipeline<Disable2FACommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PlatformDbContext _dbContext;

    public Disable2FACommandHandler(
        UserManager<ApplicationUser> userManager,
        PlatformDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        Disable2FACommand cmd,
        CancellationToken ct)
    {
        // 1. ユーザー取得
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user is null)
            return Result.Fail<Unit>("ユーザーが見つかりません");

        // 2. 2FA有効化チェック
        if (!user.IsTwoFactorEnabled)
            return Result.Fail<Unit>("二要素認証は既に無効化されています");

        // 3. パスワード検証（セキュリティレベルを下げる操作のため必須）
        var passwordValid = await _userManager.CheckPasswordAsync(user, cmd.Password);
        if (!passwordValid)
            return Result.Fail<Unit>("パスワードが正しくありません");

        // 4. リカバリーコード全削除
        var recoveryCodes = await _dbContext.TwoFactorRecoveryCodes
            .Where(c => c.UserId == user.Id)
            .ToListAsync(ct);

        _dbContext.TwoFactorRecoveryCodes.RemoveRange(recoveryCodes);

        // 5. 2FA無効化
        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        user.TwoFactorEnabledAt = null;
        user.TwoFactorRecoveryCodesRemaining = 0;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Fail<Unit>($"2FAの無効化に失敗しました: {errors}");
        }

        // 6. DbContext保存は TransactionBehavior が自動で実行

        return Result.Success(Unit.Value);
    }
}
