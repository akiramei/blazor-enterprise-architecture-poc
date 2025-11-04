using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Api.Auth.Dtos;

/// <summary>
/// Refresh Tokenリクエスト
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// 現在のAccess Token
    /// </summary>
    [Required(ErrorMessage = "Access token is required")]
    public string AccessToken { get; init; } = default!;

    /// <summary>
    /// Refresh Token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = default!;
}
