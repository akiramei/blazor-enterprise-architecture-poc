using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Infrastructure.Services;

/// <summary>
/// 現在のユーザー情報を提供するサービス（Blazor Server用）
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // 未認証の場合（AllowAnonymousAuthorizationHandlerが有効の場合）
            // 本番環境では、AllowAnonymousAuthorizationHandlerを削除し、
            // 認証が必須となるため、ここに到達しないようにする
            return Guid.Empty;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst("TenantId")?.Value;

            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }

            return null;
        }
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? "Anonymous";

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated
        ?? false;

    public ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role)
               ?? false;
    }
}
