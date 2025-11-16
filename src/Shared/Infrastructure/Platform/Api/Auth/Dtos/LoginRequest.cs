using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Platform.Api.Auth.Dtos;

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

    /// <summary>
    /// 二要素認証コード（TOTPまたはリカバリーコード）
    /// 2FA有効ユーザーの場合に必要
    /// </summary>
    public string? TwoFactorCode { get; init; }

    /// <summary>
    /// リカバリーコードを使用するか
    /// true: リカバリーコード、false: TOTPコード
    /// </summary>
    public bool IsRecoveryCode { get; init; } = false;
}
