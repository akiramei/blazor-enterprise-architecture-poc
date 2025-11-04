namespace Shared.Infrastructure.Api.Auth.Dtos;

/// <summary>
/// ログインレスポンス
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Access Token（JWT）
    /// </summary>
    public string AccessToken { get; init; } = default!;

    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; init; } = default!;

    /// <summary>
    /// Access Tokenの有効期限（UTC）
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// ユーザーID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// メールアドレス
    /// </summary>
    public string Email { get; init; } = default!;

    /// <summary>
    /// ユーザーロール
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
