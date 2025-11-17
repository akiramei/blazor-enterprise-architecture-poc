using QRCoder;

namespace Shared.Infrastructure.Services;

/// <summary>
/// QRコード生成サービスの実装
///
/// 【技術仕様】
/// - ライブラリ: QRCoder
/// - エラー訂正レベル: Q（中程度）
/// - ピクセルサイズ: 20px
/// - 出力形式: PNG（Base64エンコード）
///
/// 【Phase 2改善】
/// UI層（TwoFactorSettings.razor）から以下のコードを移動：
/// <code>
/// using var qrGenerator = new QRCodeGenerator();
/// var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
/// using var qrCode = new PngByteQRCode(qrCodeData);
/// var qrCodeImage = qrCode.GetGraphic(20);
/// return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
/// </code>
/// </summary>
public class QrCodeService : IQrCodeService
{
    /// <summary>
    /// QRコードをBase64エンコードされたPNG画像として生成
    /// </summary>
    /// <param name="content">QRコードに埋め込むコンテンツ</param>
    /// <returns>data:image/png;base64,... 形式の画像データURI</returns>
    public string GenerateQrCodeImage(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);

        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
    }
}
