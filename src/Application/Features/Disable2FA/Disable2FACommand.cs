using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.Disable2FA;

/// <summary>
/// 二要素認証（2FA）無効化コマンド
///
/// 【目的】
/// セキュリティレベルを下げる操作のため、パスワード再確認が必須。
/// 2FA無効化時は、秘密鍵とリカバリーコードをすべて削除する。
///
/// 【フロー】
/// 1. パスワード検証
/// 2. IsTwoFactorEnabled = false
/// 3. TwoFactorSecretKey = null
/// 4. TwoFactorEnabledAt = null
/// 5. リカバリーコード全削除
/// </summary>
public class Disable2FACommand : ICommand<Result<Unit>>
{
    /// <summary>
    /// 無効化対象のユーザーID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// パスワード確認（セキュリティ操作のため必須）
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
