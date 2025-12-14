using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Core.Commands;
using Shared.Application;
using Shared.Domain.Identity;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Platform.Persistence;

using System.Security.Claims;

namespace Application.Features.RefreshToken;

/// <summary>
/// Refresh Tokenコマンドハンドラー
///
/// 【Phase 3改善】
/// - AuthController.RefreshToken()から80行のビジネスロジックを移植
/// - Refresh Token Rotation実装（古いトークンを無効化）
/// - トランザクション管理をCommandPipelineに委譲
///
/// 【責務】
/// 1. Access Token検証（ClaimsPrincipal取得）
/// 2. Refresh Token検証（DB照合、有効期限確認）
/// 3. ユーザー取得
/// 4. 古いRefresh Token無効化
/// 5. 新しいトークンペア生成
/// 6. Refresh Token保存
/// </summary>
public sealed class RefreshTokenCommandHandler : CommandPipeline<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator,
        PlatformDbContext dbContext,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task<Result<RefreshTokenResult>> ExecuteAsync(
        RefreshTokenCommand cmd,
        CancellationToken ct)
    {
        // 1. Access Tokenを検証してClaimsPrincipalを取得
        var principal = _jwtTokenGenerator.ValidateToken(cmd.AccessToken);
        if (principal is null)
        {
            _logger.LogWarning("[RefreshTokenHandler] Invalid access token");
            return Result.Fail<RefreshTokenResult>("Invalid access token");
        }

        // 2. UserIdを取得
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            _logger.LogWarning("[RefreshTokenHandler] User ID not found in token");
            return Result.Fail<RefreshTokenResult>("Invalid token claims");
        }

        // 3. Refresh Tokenを検証
        var storedRefreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.Token == cmd.RefreshToken && rt.UserId == userIdClaim.Value,
                ct);

        if (storedRefreshToken is null || !storedRefreshToken.IsValid())
        {
            _logger.LogWarning("[RefreshTokenHandler] Invalid or expired refresh token. UserId={UserId}",
                userIdClaim.Value);
            return Result.Fail<RefreshTokenResult>("Invalid or expired refresh token");
        }

        // 4. ユーザー取得
        var user = await _userManager.FindByIdAsync(userIdClaim.Value);
        if (user is null)
        {
            _logger.LogWarning("[RefreshTokenHandler] User not found. UserId={UserId}", userIdClaim.Value);
            return Result.Fail<RefreshTokenResult>("User not found");
        }

        // 5. 古いRefresh Tokenを無効化（Refresh Token Rotation）
        storedRefreshToken.Revoke();

        // 6. 新しいトークンペア生成
        var newAccessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = global::Shared.Infrastructure.Authentication.RefreshToken.Create(
            newRefreshTokenValue,
            user.Id.ToString(),
            DateTime.UtcNow.AddDays(7)); // TODO: appsettings.jsonから取得

        _dbContext.RefreshTokens.Add(newRefreshToken);

        // トランザクション境界：Platform TransactionBehavior が自動的に SaveChanges/Commit/Rollback

        _logger.LogInformation("[RefreshTokenHandler] Token refresh successful. UserId={UserId}", user.Id);

        return Result.Success(new RefreshTokenResult(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenValue,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15))); // TODO: appsettings.jsonから取得
    }
}
