using Hangfire.Dashboard;
using Shared.Domain.Identity;

namespace Application.Middleware;

/// <summary>
/// Hangfire ダッシュボード認証フィルタ
///
/// 【パターン: Role-Based Authorization Filter】
///
/// 責任:
/// - Hangfireダッシュボードへのアクセスを管理者(Admin)ロールのみに制限
/// - 開発環境ではローカルホストからのアクセスを許可
///
/// セキュリティ:
/// - 本番環境では必ず認証されたAdminユーザーのみアクセス可能
/// - 開発環境ではローカルホストから認証なしでアクセス可能（開発効率化）
///
/// AI実装時の注意:
/// - 本番環境では必ずIsLocalRequest()チェックを無効化すること
/// - Adminロールのチェックは必須
/// </summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 開発環境: ローカルホストからのアクセスを許可
        if (IsLocalRequest(httpContext))
        {
            return true;
        }

        // 本番環境: 認証済みかつAdminロールを持つユーザーのみ許可
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole(Roles.Admin);
    }

    /// <summary>
    /// ローカルホストからのリクエストかどうかを判定
    /// </summary>
    private static bool IsLocalRequest(HttpContext context)
    {
        var connection = context.Connection;

        // ローカルIPアドレスからのアクセス
        if (connection.RemoteIpAddress != null)
        {
            // ローカルループバックアドレス (127.0.0.1 または ::1)
            if (connection.LocalIpAddress != null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }

            // IPv4 ローカルホスト
            return connection.RemoteIpAddress.ToString() == "127.0.0.1" ||
                   connection.RemoteIpAddress.ToString() == "::1";
        }

        return false;
    }
}
