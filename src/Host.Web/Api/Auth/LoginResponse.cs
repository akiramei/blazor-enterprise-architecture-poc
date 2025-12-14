namespace Application.Api.Auth;

/// <summary>
/// ログインレスポンス
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Access Token（JWT）
    /// 2FA要求時はnull
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh Token
    /// 2FA要求時はnull
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Access Tokenの有効期限（UTC）
    /// 2FA要求時はnull
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// ユーザーID
    /// 2FA要求時はnull
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// メールアドレス
    /// 2FA要求時はnull
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// ユーザーロール
    /// 2FA要求時は空配列
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// 二要素認証が必要か
    /// true: 2FAコードの入力が必要（TwoFactorCodeを含めて再度ログインリクエスト）
    /// false: ログイン成功（トークンが発行されている）
    /// </summary>
    public bool Requires2FA { get; init; } = false;
}
