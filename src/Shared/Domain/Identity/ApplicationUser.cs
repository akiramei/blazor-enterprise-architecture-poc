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
}
