using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.Login;

/// <summary>
/// ログインコマンド
///
/// 【Phase 3改善】
/// - AuthController.Login()からビジネスロジックを分離
/// - CQRS/MediatRパターン適用
/// - トランザクション管理をCommandPipelineに委譲
///
/// 【責務】
/// - パスワード認証
/// - 2FA検証（TOTP/RecoveryCode）
/// - JWT Token生成
/// </summary>
public sealed class LoginCommand : ICommand<Result<LoginResult>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? TwoFactorCode { get; init; }
    public bool IsRecoveryCode { get; init; }
}

/// <summary>
/// ログイン結果
/// </summary>
public sealed record LoginResult
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public List<string> Roles { get; init; } = new();
    public bool Requires2FA { get; init; }

    /// <summary>
    /// 2FA要求レスポンス生成
    /// </summary>
    public static LoginResult Create2FARequired()
    {
        return new LoginResult
        {
            AccessToken = null,
            RefreshToken = null,
            ExpiresAt = null,
            UserId = null,
            Email = null,
            Roles = new List<string>(),
            Requires2FA = true
        };
    }

    /// <summary>
    /// 成功レスポンス生成
    /// </summary>
    public static LoginResult CreateSuccess(
        string accessToken,
        string refreshToken,
        DateTime expiresAt,
        Guid userId,
        string email,
        List<string> roles)
    {
        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = userId,
            Email = email,
            Roles = roles,
            Requires2FA = false
        };
    }
}
