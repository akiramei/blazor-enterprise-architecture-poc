namespace Shared.Infrastructure.Services;

/// <summary>
/// QRコード生成サービスのインターフェース
///
/// 【目的】
/// - UI層からQRCoder依存を排除
/// - QRコード生成ロジックをInfrastructure層に配置
/// - テスタビリティの向上（モック可能）
///
/// 【Phase 2改善】
/// Before: TwoFactorSettings.razor内でQRCodeGeneratorを直接使用
/// After: IQrCodeServiceに委譲
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// QRコードをBase64エンコードされたPNG画像として生成
    /// </summary>
    /// <param name="content">QRコードに埋め込むコンテンツ（通常はotpauth://形式のURI）</param>
    /// <returns>data:image/png;base64,... 形式の画像データURI</returns>
    string GenerateQrCodeImage(string content);
}
