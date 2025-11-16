using Hangfire.Dashboard;
using Shared.Domain.Identity;

namespace Application.Middleware;

/// <summary>
/// Hangfire 繝繝・す繝･繝懊・繝芽ｪ榊庄繝輔ぅ繝ｫ繧ｿ
///
/// 縲舌ヱ繧ｿ繝ｼ繝ｳ: Role-Based Authorization Filter縲・
///
/// 雋ｬ蜍・
/// - Hangfire繝繝・す繝･繝懊・繝峨∈縺ｮ繧｢繧ｯ繧ｻ繧ｹ繧堤ｮ｡逅・・Admin)繝ｭ繝ｼ繝ｫ縺ｮ縺ｿ縺ｫ蛻ｶ髯・
/// - 髢狗匱迺ｰ蠅・〒縺ｯ繝ｭ繝ｼ繧ｫ繝ｫ繝帙せ繝医°繧峨・繧｢繧ｯ繧ｻ繧ｹ繧定ｨｱ蜿ｯ
///
/// 繧ｻ繧ｭ繝･繝ｪ繝・ぅ:
/// - 譛ｬ逡ｪ迺ｰ蠅・〒縺ｯ蠢・★隱崎ｨｼ縺輔ｌ縺蘗dmin繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ縺ｿ繧｢繧ｯ繧ｻ繧ｹ蜿ｯ閭ｽ
/// - 髢狗匱迺ｰ蠅・〒縺ｯ繝ｭ繝ｼ繧ｫ繝ｫ繝帙せ繝医°繧芽ｪ崎ｨｼ縺ｪ縺励〒繧｢繧ｯ繧ｻ繧ｹ蜿ｯ閭ｽ・磯幕逋ｺ蜉ｹ邇・喧・・
///
/// AI螳溯｣・凾縺ｮ豕ｨ諢・
/// - 譛ｬ逡ｪ迺ｰ蠅・〒縺ｯ蠢・★IsLocalRequest()繝√ぉ繝・け繧堤┌蜉ｹ蛹悶☆繧九％縺ｨ
/// - Admin繝ｭ繝ｼ繝ｫ縺ｮ繝√ぉ繝・け縺ｯ蠢・・
/// </summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 髢狗匱迺ｰ蠅・ 繝ｭ繝ｼ繧ｫ繝ｫ繝帙せ繝医°繧峨・繧｢繧ｯ繧ｻ繧ｹ繧定ｨｱ蜿ｯ
        if (IsLocalRequest(httpContext))
        {
            return true;
        }

        // 譛ｬ逡ｪ迺ｰ蠅・ 隱崎ｨｼ貂医∩縺九▽Admin繝ｭ繝ｼ繝ｫ繧呈戟縺､繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ縺ｿ險ｱ蜿ｯ
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole(Roles.Admin);
    }

    /// <summary>
    /// 繝ｭ繝ｼ繧ｫ繝ｫ繝帙せ繝医°繧峨・繝ｪ繧ｯ繧ｨ繧ｹ繝医°縺ｩ縺・°繧貞愛螳・
    /// </summary>
    private static bool IsLocalRequest(HttpContext context)
    {
        var connection = context.Connection;

        // 繝ｭ繝ｼ繧ｫ繝ｫIP繧｢繝峨Ξ繧ｹ縺九ｉ縺ｮ繧｢繧ｯ繧ｻ繧ｹ
        if (connection.RemoteIpAddress != null)
        {
            // 繝ｭ繝ｼ繧ｫ繝ｫ繝ｫ繝ｼ繝励ヰ繝・け繧｢繝峨Ξ繧ｹ (127.0.0.1 縺ｾ縺溘・ ::1)
            if (connection.LocalIpAddress != null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }

            // IPv4 繝ｭ繝ｼ繧ｫ繝ｫ繝帙せ繝・
            return connection.RemoteIpAddress.ToString() == "127.0.0.1" ||
                   connection.RemoteIpAddress.ToString() == "::1";
        }

        return false;
    }
}
