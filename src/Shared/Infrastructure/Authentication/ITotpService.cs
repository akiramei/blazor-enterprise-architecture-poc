namespace Shared.Infrastructure.Authentication;

/// <summary>
/// TOTP（Time-based One-Time Password）サービス
///
/// 用途:
/// - Google Authenticator、Microsoft Authenticator等の認証アプリと連携
/// - RFC 6238準拠のTOTP実装
///
/// セキュリティ設計:
/// - 秘密鍵はBase32エンコード（認証アプリとの互換性）
/// - 時間ウィンドウ: 30秒（標準）
/// - コード長: 6桁（標準）
/// - 検証時に前後1分の許容（タイムドリフト対策）
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// TOTP秘密鍵を生成
    /// </summary>
    /// <returns>Base32エンコードされた秘密鍵</returns>
    string GenerateSecretKey();

    /// <summary>
    /// QRコードURIを生成
    /// 認証アプリでスキャンするためのotpauth://形式のURI
    /// </summary>
    /// <param name="email">ユーザーのメールアドレス（認証アプリに表示される）</param>
    /// <param name="secretKey">TOTP秘密鍵</param>
    /// <returns>otpauth://totp/ 形式のURI</returns>
    string GenerateQrCodeUri(string email, string secretKey);

    /// <summary>
    /// TOTPコードを検証
    /// </summary>
    /// <param name="secretKey">TOTP秘密鍵</param>
    /// <param name="code">ユーザーが入力した6桁のコード</param>
    /// <returns>コードが有効な場合true</returns>
    bool ValidateCode(string secretKey, string code);

    /// <summary>
    /// リカバリーコードを生成
    /// </summary>
    /// <param name="count">生成するコード数（デフォルト: 10）</param>
    /// <returns>リカバリーコードのリスト</returns>
    List<string> GenerateRecoveryCodes(int count = 10);
}
