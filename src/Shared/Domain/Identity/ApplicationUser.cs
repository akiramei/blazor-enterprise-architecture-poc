using Microsoft.AspNetCore.Identity;

namespace Shared.Domain.Identity;

/// <summary>
/// アプリケーションユーザー
/// ASP.NET Core Identity の IdentityUser を継承
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// 表示名
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// 最終更新日時（UTC）
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// アクティブフラグ
    /// </summary>
    public bool IsActive { get; set; } = true;

    // ============================================
    // 二要素認証（2FA）関連プロパティ
    // ============================================

    /// <summary>
    /// 二要素認証が有効化されているか
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// TOTP秘密鍵（Base32エンコード）
    /// セキュリティ強化版ではData Protectionで暗号化して保存することを推奨
    /// </summary>
    public string? TwoFactorSecretKey { get; set; }

    /// <summary>
    /// 二要素認証を有効化した日時（UTC）
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; set; }

    /// <summary>
    /// 残りのリカバリーコード数
    /// </summary>
    public int TwoFactorRecoveryCodesRemaining { get; set; } = 0;
}
