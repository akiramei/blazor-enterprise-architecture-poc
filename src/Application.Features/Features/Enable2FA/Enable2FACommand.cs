using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.Enable2FA;

/// <summary>
/// 二要素認証（2FA）有効化コマンド
/// TOTP秘密鍵生成、リカバリーコード生成、DB保存を実行
/// </summary>
public sealed class Enable2FACommand : ICommand<Result<Enable2FAResult>>
{
    /// <summary>
    /// 2FAを有効化するユーザーID
    /// </summary>
    public Guid UserId { get; init; }
}

/// <summary>
/// 2FA有効化結果
/// </summary>
public sealed record Enable2FAResult(
    /// <summary>
    /// TOTP秘密鍵（Base32エンコード）
    /// ユーザーに一度だけ表示し、認証アプリに登録させる
    /// </summary>
    string SecretKey,

    /// <summary>
    /// QRコードURI（otpauth://形式）
    /// 認証アプリでスキャン可能な形式
    /// </summary>
    string QrCodeUri,

    /// <summary>
    /// リカバリーコード（平文）
    /// ユーザーに一度だけ表示し、安全に保存させる
    /// DB保存時はBCryptでハッシュ化される
    /// </summary>
    List<string> RecoveryCodes);
