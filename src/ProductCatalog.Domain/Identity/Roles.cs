namespace ProductCatalog.Domain.Identity;

/// <summary>
/// アプリケーションで使用するロール定数
/// </summary>
public static class Roles
{
    /// <summary>
    /// 管理者ロール（すべての操作が可能）
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// 一般ユーザーロール（読み取りのみ可能）
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// すべてのロール
    /// </summary>
    public static readonly string[] All = { Admin, User };
}
