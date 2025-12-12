using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Core.Commands;
using Shared.Application;
using Shared.Domain.Identity;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Platform.Persistence;

namespace Application.Features.Login;

/// <summary>
/// ログインコマンドハンドラー
///
/// 【Phase 3改善】
/// - AuthController.Login()から130行のビジネスロジックを移植
/// - トランザクション管理をCommandPipelineに委譲
/// - エラーハンドリングをResult&lt;T&gt;パターンに統一
///
/// 【責務】
/// 1. ユーザー検索（Email）
/// 2. パスワード検証（SignInManager）
/// 3. 2FA検証（TOTP/RecoveryCode）
/// 4. JWT Token生成
/// 5. Refresh Token保存
/// 6. ロール取得
/// </summary>
public sealed class LoginCommandHandler : CommandPipeline<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITotpService _totpService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITotpService totpService,
        IJwtTokenGenerator jwtTokenGenerator,
        PlatformDbContext dbContext,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _totpService = totpService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task<Result<LoginResult>> ExecuteAsync(
        LoginCommand cmd,
        CancellationToken ct)
    {
        // 1. ユーザー検索
        var user = await _userManager.FindByEmailAsync(cmd.Email);
        if (user is null)
        {
            _logger.LogWarning("[LoginHandler] User not found. Email={Email}", cmd.Email);
            return Result.Fail<LoginResult>("Invalid email or password");
        }

        // 2. パスワード検証
        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user, cmd.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("[LoginHandler] Password verification failed. Email={Email}, LockedOut={LockedOut}",
                cmd.Email, signInResult.IsLockedOut);

            return Result.Fail<LoginResult>(
                signInResult.IsLockedOut
                    ? "Account is locked out. Please try again later."
                    : "Invalid email or password");
        }

        // 3. 二要素認証（2FA）検証
        if (user.IsTwoFactorEnabled)
        {
            var twoFactorResult = await Verify2FA(user, cmd, ct);

            if (!twoFactorResult.IsSuccess)
            {
                return Result.Fail<LoginResult>(twoFactorResult.Error ?? "Invalid two-factor authentication code");
            }

            // 2FA要求レスポンス（コード未提供の場合）
            if (twoFactorResult.Value is not null)
            {
                return Result.Success(twoFactorResult.Value);
            }
        }

        // 4. JWT Access Token生成
        var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        // 5. Refresh Token保存
        var refreshToken = global::Shared.Infrastructure.Authentication.RefreshToken.Create(
            refreshTokenValue,
            user.Id.ToString(),
            DateTime.UtcNow.AddDays(7)); // TODO: appsettings.jsonから取得

        _dbContext.RefreshTokens.Add(refreshToken);

        // 6. ユーザーロール取得
        var roles = await _userManager.GetRolesAsync(user);

        // トランザクション境界：Platform TransactionBehavior が自動的に SaveChanges/Commit/Rollback

        _logger.LogInformation("[LoginHandler] Login successful. UserId={UserId}, Email={Email}",
            user.Id, user.Email);

        return Result.Success(LoginResult.CreateSuccess(
            accessToken: accessToken,
            refreshToken: refreshTokenValue,
            expiresAt: DateTime.UtcNow.AddMinutes(15), // TODO: appsettings.jsonから取得
            userId: user.Id,
            email: user.Email!,
            roles: roles.ToList()));
    }

    /// <summary>
    /// 2FA検証（TOTP/RecoveryCode）
    /// </summary>
    /// <returns>
    /// - Success + Value=null: 2FA検証成功
    /// - Success + Value=LoginResult: 2FA要求レスポンス
    /// - Fail: 2FA検証失敗
    /// </returns>
    private async Task<Result<LoginResult?>> Verify2FA(
        ApplicationUser user,
        LoginCommand cmd,
        CancellationToken ct)
    {
        // 2FAコード未提供 → 2FA要求レスポンス
        if (string.IsNullOrEmpty(cmd.TwoFactorCode))
        {
            _logger.LogInformation("[LoginHandler] 2FA required. UserId={UserId}", user.Id);
            return Result.Success<LoginResult?>(LoginResult.Create2FARequired());
        }

        // リカバリーコード検証
        if (cmd.IsRecoveryCode)
        {
            var verifyResult = await VerifyRecoveryCode(user, cmd.TwoFactorCode, ct);
            if (!verifyResult.IsSuccess)
                return Result.Fail<LoginResult?>(verifyResult.Error ?? "Invalid recovery code");

            _logger.LogInformation("[LoginHandler] Login with recovery code successful. UserId={UserId}", user.Id);
            return Result.Success<LoginResult?>(null); // 検証成功
        }

        // TOTP検証
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey) ||
            !_totpService.ValidateCode(user.TwoFactorSecretKey, cmd.TwoFactorCode))
        {
            _logger.LogWarning("[LoginHandler] Invalid 2FA code. UserId={UserId}", user.Id);
            return Result.Fail<LoginResult?>("Invalid two-factor authentication code");
        }

        _logger.LogInformation("[LoginHandler] Login with TOTP code successful. UserId={UserId}", user.Id);
        return Result.Success<LoginResult?>(null); // 検証成功
    }

    /// <summary>
    /// リカバリーコード検証
    /// </summary>
    private async Task<Result> VerifyRecoveryCode(
        ApplicationUser user,
        string code,
        CancellationToken ct)
    {
        var recoveryCode = await _dbContext.TwoFactorRecoveryCodes
            .FirstOrDefaultAsync(c =>
                c.UserId == user.Id && !c.IsUsed,
                ct);

        if (recoveryCode is null || !recoveryCode.Verify(code))
        {
            _logger.LogWarning("[LoginHandler] Invalid recovery code. UserId={UserId}", user.Id);
            return Result.Fail("Invalid recovery code");
        }

        // リカバリーコードを使用済みとしてマーク
        recoveryCode.MarkAsUsed();
        user.TwoFactorRecoveryCodesRemaining = Math.Max(0, user.TwoFactorRecoveryCodesRemaining - 1);
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}
