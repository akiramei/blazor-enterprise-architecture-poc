namespace Shared.Infrastructure.Platform.Api.Auth.Dtos;

/// <summary>
/// 2FA有効化レスポンス
/// QRコードとリカバリーコードを返す
/// </summary>
/// <param name="SecretKey">TOTP秘密鍵（手動入力用）</param>
/// <param name="QrCodeUri">QRコードURI（認証アプリでスキャン）</param>
/// <param name="RecoveryCodes">リカバリーコードのリスト（大切に保管）</param>
public sealed record Enable2FAResponse(
    string SecretKey,
    string QrCodeUri,
    List<string> RecoveryCodes
);
