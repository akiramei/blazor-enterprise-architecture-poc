using System.Security.Cryptography;
using OtpNet;

namespace Shared.Infrastructure.Authentication;

/// <summary>
/// TOTP（Time-based One-Time Password）サービスの実装
///
/// 依存ライブラリ:
/// - Otp.NET: RFC 6238準拠のTOTP実装
///
/// 設定値:
/// - アルゴリズム: SHA-1（RFC 6238標準）
/// - 時間ステップ: 30秒
/// - コード長: 6桁
/// - 検証ウィンドウ: 前後1分（タイムドリフト対策）
/// </summary>
public sealed class TotpService : ITotpService
{
    private const string Issuer = "VSASample";
    private const int SecretKeyLength = 20; // バイト数（160ビット）

    /// <summary>
    /// TOTP秘密鍵を生成
    /// </summary>
    /// <returns>Base32エンコードされた秘密鍵</returns>
    public string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(SecretKeyLength);
        return Base32Encoding.ToString(key);
    }

    /// <summary>
    /// QRコードURIを生成
    /// </summary>
    /// <param name="email">ユーザーのメールアドレス</param>
    /// <param name="secretKey">TOTP秘密鍵</param>
    /// <returns>otpauth://totp/ 形式のURI</returns>
    public string GenerateQrCodeUri(string email, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Secret key cannot be empty", nameof(secretKey));

        // URLエンコードしてスペースや特殊文字に対応
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedIssuer = Uri.EscapeDataString(Issuer);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}";
    }

    /// <summary>
    /// TOTPコードを検証
    /// </summary>
    /// <param name="secretKey">TOTP秘密鍵</param>
    /// <param name="code">ユーザーが入力した6桁のコード</param>
    /// <returns>コードが有効な場合true</returns>
    public bool ValidateCode(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretKeyBytes);

            // 前後1分（2ステップ）の許容でタイムドリフトに対応
            // previous: 1 = 30秒前まで許容
            // future: 1 = 30秒後まで許容
            var verificationWindow = new VerificationWindow(previous: 1, future: 1);

            return totp.VerifyTotp(code, out _, verificationWindow);
        }
        catch (Exception)
        {
            // Base32デコードエラーやその他の例外の場合はfalse
            return false;
        }
    }

    /// <summary>
    /// リカバリーコードを生成
    /// </summary>
    /// <param name="count">生成するコード数</param>
    /// <returns>リカバリーコードのリスト</returns>
    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than zero", nameof(count));

        var codes = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);

            // Base64エンコード後、URLに安全でない文字を削除し、10文字に切り詰め
            var code = Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                [..10];

            codes.Add(code);
        }

        return codes;
    }
}
