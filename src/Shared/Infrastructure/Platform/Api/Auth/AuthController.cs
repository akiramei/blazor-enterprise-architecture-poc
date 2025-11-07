using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Domain.Identity;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Platform.Persistence;
using Shared.Infrastructure.Platform.Api.Auth.Dtos;

namespace Shared.Infrastructure.Platform.Api.Auth;

/// <summary>
/// 認証API（Login, RefreshToken）
///
/// 【パターン: JWT Bearer認証 + Refresh Token】
///
/// このControllerは、REST APIクライアント（外部システム、モバイルアプリ、SPA等）向けの
/// JWT Bearer認証エンドポイントを提供します。
///
/// ## 設計判断
///
/// ### なぜJWT Bearer + Refresh Tokenなのか？
/// - **ステートレス**: サーバー側でセッション管理が不要
/// - **スケーラブル**: 水平スケールが容易
/// - **モバイル・SPA対応**: Cookie非依存の認証方式
/// - **セキュリティ**: Access Token短命(15分) + Refresh Token長命(7日)のバランス
///
/// ### Blazor Server (Cookie認証) との併用
/// このアプリケーションでは、以下2つの認証方式を並行稼働させています:
/// - **Blazor Server UI**: Cookie認証（セッション管理、SignalR利用）
/// - **REST API**: JWT Bearer認証（ステートレス、外部連携）
///
/// ## APIクライアントへの契約
///
/// 1. **ログインフロー**:
///    - `/api/v1/auth/login` でAccess Token + Refresh Tokenを取得
///    - Access TokenをすべてのAPIリクエストの `Authorization: Bearer {token}` ヘッダーに含める
///
/// 2. **トークン更新フロー**:
///    - Access Token期限切れ（15分）時は `/api/v1/auth/refresh` で更新
///    - Refresh Tokenも期限切れ（7日）の場合は再ログイン
///
/// 3. **セキュリティ要件**:
///    - トークンは安全に保存（メモリ、暗号化ストレージ）
///    - LocalStorage/SessionStorageに保存しない（XSS脆弱性）
///    - 機密情報をログに出力しない
///
/// ## AI実装時の注意
///
/// - **Refresh Token Rotation**: 使用済みRefresh Tokenは無効化（セキュリティ強化）
/// - **レート制限**: 認証系エンドポイントは 5 req/min（ブルートフォース攻撃対策）
/// - **ロックアウト**: 5回失敗で5分間ロック（ASP.NET Core Identity設定）
/// - **エラーレスポンス**: RFC 7807 Problem Details形式で統一
/// - **ログ出力**: パスワードやトークンをログに記録しない
///
/// ## 関連ドキュメント
/// - docs/patterns/REST-API-DESIGN-GUIDE.md - 認証フロー詳細
/// - docs/patterns/API-CLIENT-CONTRACT.md - クライアント契約
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]  // ❗ 認証不要（ログイン前のユーザーがアクセス）
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator,
        PlatformDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// ログイン
    /// </summary>
    /// <param name="request">ログインリクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Access TokenとRefresh Token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // ユーザー検索
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: User not found. Email={Email}", request.Email);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid email or password",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // パスワード検証
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed: Invalid password. Email={Email}", request.Email);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = result.IsLockedOut
                    ? "Account is locked out. Please try again later."
                    : "Invalid email or password",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // JWT Access Token生成
        var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        // Refresh Token保存
        var refreshToken = Authentication.RefreshToken.Create(
            refreshTokenValue,
            user.Id.ToString(),
            DateTime.UtcNow.AddDays(7)); // appsettings.jsonから取得すべきだが簡易実装

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ユーザーロール取得
        var roles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("Login successful. UserId={UserId}, Email={Email}", user.Id, user.Email);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // appsettings.jsonから取得すべきだが簡易実装
            UserId = user.Id,
            Email = user.Email!,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Refresh Token（Access Token更新）
    /// </summary>
    /// <param name="request">Refresh Tokenリクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>新しいAccess TokenとRefresh Token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        // Access Tokenを検証してClaimsPrincipalを取得
        var principal = _jwtTokenGenerator.ValidateToken(request.AccessToken);
        if (principal is null)
        {
            _logger.LogWarning("Refresh token failed: Invalid access token");
            return Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "Invalid access token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // UserIdを取得
        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            _logger.LogWarning("Refresh token failed: User ID not found in token");
            return Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "Invalid token claims",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Refresh Tokenを検証
        var storedRefreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userIdClaim.Value, cancellationToken);

        if (storedRefreshToken is null || !storedRefreshToken.IsValid())
        {
            _logger.LogWarning("Refresh token failed: Invalid or expired refresh token. UserId={UserId}", userIdClaim.Value);
            return Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "Invalid or expired refresh token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // ユーザー取得
        var user = await _userManager.FindByIdAsync(userIdClaim.Value);
        if (user is null)
        {
            _logger.LogWarning("Refresh token failed: User not found. UserId={UserId}", userIdClaim.Value);
            return Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "User not found",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // 古いRefresh Tokenを無効化
        storedRefreshToken.Revoke();

        // 新しいトークン生成
        var newAccessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = Authentication.RefreshToken.Create(
            newRefreshTokenValue,
            user.Id.ToString(),
            DateTime.UtcNow.AddDays(7));

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refresh successful. UserId={UserId}", user.Id);

        return Ok(new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }
}
