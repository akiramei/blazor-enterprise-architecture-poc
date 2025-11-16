namespace Shared.Infrastructure.Platform.Api.Auth.Dtos;

/// <summary>
/// 2FA検証リクエスト
/// 認証アプリから生成された6桁のコードで2FA有効化を確定
/// </summary>
/// <param name="Code">6桁のTOTPコード</param>
public sealed record Verify2FARequest(string Code);
