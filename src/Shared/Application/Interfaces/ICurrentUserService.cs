using System.Security.Claims;

namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// 現在のユーザー情報を提供するサービス
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// テナントID（マルチテナント対応用）
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// ユーザー名
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// 認証済みかどうか
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// 指定されたロールに属しているか
    /// </summary>
    bool IsInRole(string role);
}
