using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.Verify2FA;

/// <summary>
/// 二要素認証（2FA）検証・確定コマンド
///
/// 【目的】
/// Enable2FACommandで生成された秘密鍵を使用し、
/// ユーザーが認証アプリに正しく登録できたかをTOTPコードで検証する。
/// 検証成功後、2FAを正式に有効化する。
///
/// 【フロー】
/// 1. Enable2FACommand実行 → 秘密鍵・QRコード・リカバリーコード取得
/// 2. ユーザーが認証アプリに秘密鍵を登録
/// 3. Verify2FACommand実行 → TOTPコード検証
/// 4. 検証成功 → IsTwoFactorEnabled = true
/// </summary>
public class Verify2FACommand : ICommand<Result<Unit>>
{
    /// <summary>
    /// 検証対象のユーザーID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 認証アプリに表示されている6桁のTOTPコード
    /// </summary>
    public string VerificationCode { get; init; } = string.Empty;
}
