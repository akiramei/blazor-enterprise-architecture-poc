namespace ProductCatalog.Infrastructure.Authentication;

/// <summary>
/// JWT設定
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT署名用の秘密鍵（最小256ビット = 32文字）
    /// </summary>
    public string SecretKey { get; init; } = default!;

    /// <summary>
    /// トークン発行者
    /// </summary>
    public string Issuer { get; init; } = default!;

    /// <summary>
    /// トークン対象者
    /// </summary>
    public string Audience { get; init; } = default!;

    /// <summary>
    /// Access Tokenの有効期限（分）
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    /// <summary>
    /// Refresh Tokenの有効期限（日）
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
