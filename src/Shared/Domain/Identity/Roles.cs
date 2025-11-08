namespace Shared.Domain.Identity;

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
    /// 購買申請者ロール（購買申請の作成・キャンセル可能）
    /// </summary>
    public const string Requester = "Requester";

    /// <summary>
    /// 承認者ロール（購買申請の承認・却下可能）
    /// </summary>
    public const string Approver = "Approver";

    /// <summary>
    /// すべてのロール
    /// </summary>
    public static readonly string[] All = { Admin, User, Requester, Approver };
}
