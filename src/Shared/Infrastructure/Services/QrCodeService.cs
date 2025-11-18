using Microsoft.Extensions.Logging;
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
/// - 最大コンテンツ長: 2048文字
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
///
/// 【入力検証】
/// - null/空文字列/空白文字のみは拒否
/// - 最大長を超える場合は拒否
/// - QRCoder例外を適切に処理
/// </summary>
public class QrCodeService : IQrCodeService
{
    /// <summary>
    /// QRコードに埋め込めるコンテンツの最大文字数
    ///
    /// 【制限理由】
    /// QRコードVersion 40 + ECCLevel.Q（中程度のエラー訂正）の理論上の上限は約1852文字（英数字）。
    /// 安全マージンを考慮して2048文字を上限とする。
    /// </summary>
    private const int MaxContentLength = 2048;

    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// QRコードをBase64エンコードされたPNG画像として生成
    /// </summary>
    /// <param name="content">QRコードに埋め込むコンテンツ</param>
    /// <returns>data:image/png;base64,... 形式の画像データURI</returns>
    /// <exception cref="ArgumentNullException">contentがnullの場合</exception>
    /// <exception cref="ArgumentException">contentが空文字列、空白文字のみ、または最大長を超える場合</exception>
    /// <exception cref="InvalidOperationException">QRコード生成に失敗した場合</exception>
    public string GenerateQrCodeImage(string content)
    {
        // 入力検証: null チェック
        if (content is null)
        {
            _logger.LogError("QRコード生成に失敗: contentパラメータがnullです");
            throw new ArgumentNullException(nameof(content), "QRコードのコンテンツはnullにできません。");
        }

        // 入力検証: 空文字列・空白文字チェック
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogError("QRコード生成に失敗: contentパラメータが空文字列または空白文字のみです");
            throw new ArgumentException("QRコードのコンテンツは空文字列または空白文字のみにできません。", nameof(content));
        }

        // 入力検証: 最大長チェック
        if (content.Length > MaxContentLength)
        {
            _logger.LogError(
                "QRコード生成に失敗: contentパラメータが最大長を超えています。長さ={ContentLength}, 最大長={MaxLength}",
                content.Length,
                MaxContentLength);
            throw new ArgumentException(
                $"QRコードのコンテンツは{MaxContentLength}文字以内にする必要があります。(現在: {content.Length}文字)",
                nameof(content));
        }

        try
        {
            // QRコード生成
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            _logger.LogDebug("QRコードを正常に生成しました。コンテンツ長={ContentLength}", content.Length);

            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
        {
            // QRCoder からの予期しない例外をキャッチ
            _logger.LogError(
                ex,
                "QRコード生成中に予期しないエラーが発生しました。コンテンツ長={ContentLength}",
                content.Length);
            throw new InvalidOperationException(
                "QRコードの生成中にエラーが発生しました。詳細はログを確認してください。",
                ex);
        }
    }
}
