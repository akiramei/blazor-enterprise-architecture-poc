using System.Security.Claims;

namespace ProductCatalog.Web.Infrastructure.Models;

/// <summary>
/// セッション状態 - 現在のユーザー情報を保持
///
/// 設計方針:
/// - IAppContextのフロントエンド版として機能
/// - 認証状態の変更を購読可能
/// - 不変オブジェクトとして実装（record）
/// </summary>
public sealed record SessionState
{
    /// <summary>
    /// 認証状態
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// 現在のユーザーID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 現在のユーザー名
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// ユーザーのメールアドレス
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// ユーザーのロール
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    /// ClaimsPrincipal（詳細情報用）
    /// </summary>
    public ClaimsPrincipal? User { get; init; }

    /// <summary>
    /// テナントID（マルチテナント対応）
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// 初期化中フラグ
    /// </summary>
    public bool IsLoading { get; init; }

    public SessionState()
    {
        UserName = "Anonymous";
        Roles = Array.Empty<string>();
    }

    /// <summary>
    /// 匿名ユーザーの初期状態
    /// </summary>
    public static SessionState Anonymous => new()
    {
        IsAuthenticated = false,
        UserId = Guid.Empty,
        UserName = "Anonymous",
        Email = null,
        Roles = Array.Empty<string>(),
        User = null,
        TenantId = null,
        IsLoading = false
    };

    /// <summary>
    /// 指定されたロールに所属しているかを判定
    /// </summary>
    public bool IsInRole(string role) => Roles.Contains(role);

    /// <summary>
    /// 複数のロールのいずれかに所属しているかを判定
    /// </summary>
    public bool IsInAnyRole(params string[] roles) => roles.Any(r => Roles.Contains(r));
}
