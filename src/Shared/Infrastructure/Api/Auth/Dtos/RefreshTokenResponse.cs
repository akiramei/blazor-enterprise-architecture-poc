namespace ProductCatalog.Web.Features.Api.V1.Auth.Dtos;

/// <summary>
/// Refresh Tokenレスポンス
/// </summary>
public sealed record RefreshTokenResponse
{
    /// <summary>
    /// 新しいAccess Token（JWT）
    /// </summary>
    public string AccessToken { get; init; } = default!;

    /// <summary>
    /// 新しいRefresh Token
    /// </summary>
    public string RefreshToken { get; init; } = default!;

    /// <summary>
    /// Access Tokenの有効期限（UTC）
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}
