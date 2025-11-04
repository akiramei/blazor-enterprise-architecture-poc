using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Api.Auth.Dtos;

/// <summary>
/// ログインリクエスト
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// メールアドレス
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = default!;

    /// <summary>
    /// パスワード
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; init; } = default!;
}
