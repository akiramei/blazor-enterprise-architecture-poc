using System.Security.Claims;

namespace Application.Infrastructure.Models;

/// <summary>
/// 繧ｻ繝・す繝ｧ繝ｳ迥ｶ諷・- 迴ｾ蝨ｨ縺ｮ繝ｦ繝ｼ繧ｶ繝ｼ諠・ｱ繧剃ｿ晄戟
///
/// 險ｭ險域婿驥・
/// - IAppContext縺ｮ繝輔Ο繝ｳ繝医お繝ｳ繝臥沿縺ｨ縺励※讖溯・
/// - 隱崎ｨｼ迥ｶ諷九・螟画峩繧定ｳｼ隱ｭ蜿ｯ閭ｽ
/// - 荳榊､峨が繝悶ず繧ｧ繧ｯ繝医→縺励※螳溯｣・ｼ・ecord・・
/// </summary>
public sealed record SessionState
{
    /// <summary>
    /// 隱崎ｨｼ迥ｶ諷・
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// 迴ｾ蝨ｨ縺ｮ繝ｦ繝ｼ繧ｶ繝ｼID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 迴ｾ蝨ｨ縺ｮ繝ｦ繝ｼ繧ｶ繝ｼ蜷・
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// 繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ繝｡繝ｼ繝ｫ繧｢繝峨Ξ繧ｹ
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ繝ｭ繝ｼ繝ｫ
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    /// ClaimsPrincipal・郁ｩｳ邏ｰ諠・ｱ逕ｨ・・
    /// </summary>
    public ClaimsPrincipal? User { get; init; }

    /// <summary>
    /// 繝・リ繝ｳ繝・D・医・繝ｫ繝√ユ繝翫Φ繝亥ｯｾ蠢懶ｼ・
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// 蛻晄悄蛹紋ｸｭ繝輔Λ繧ｰ
    /// </summary>
    public bool IsLoading { get; init; }

    public SessionState()
    {
        UserName = "Anonymous";
        Roles = Array.Empty<string>();
    }

    /// <summary>
    /// 蛹ｿ蜷阪Θ繝ｼ繧ｶ繝ｼ縺ｮ蛻晄悄迥ｶ諷・
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
    /// 謖・ｮ壹＆繧後◆繝ｭ繝ｼ繝ｫ縺ｫ謇螻槭＠縺ｦ縺・ｋ縺九ｒ蛻､螳・
    /// </summary>
    public bool IsInRole(string role) => Roles.Contains(role);

    /// <summary>
    /// 隍・焚縺ｮ繝ｭ繝ｼ繝ｫ縺ｮ縺・★繧後°縺ｫ謇螻槭＠縺ｦ縺・ｋ縺九ｒ蛻､螳・
    /// </summary>
    public bool IsInAnyRole(params string[] roles) => roles.Any(r => Roles.Contains(r));
}
