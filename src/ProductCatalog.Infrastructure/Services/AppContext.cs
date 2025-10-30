using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Infrastructure.Services;

/// <summary>
/// アプリケーションコンテキストの実装
///
/// 【パターン: AppContext/Kernel実装】
///
/// 設計方針:
/// - HttpContext経由でリクエストスコープの情報を取得
/// - Lazy初期化によるパフォーマンス最適化
/// - 未認証時は安全なデフォルト値を返却
/// - テスト容易性を確保（IAppContextとして抽象化）
///
/// 実装詳細:
/// - ICurrentUserServiceとICorrelationIdAccessorの機能を統合
/// - リクエストメタデータ（開始時刻、パス、メソッド等）を追加
/// - 環境情報（環境名、ホスト名、アプリ名）を追加
/// </summary>
public sealed class AppContext : IAppContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    // Lazy初期化でパフォーマンス最適化
    private readonly Lazy<Guid> _userId;
    private readonly Lazy<string> _userName;
    private readonly Lazy<Guid?> _tenantId;
    private readonly Lazy<bool> _isAuthenticated;
    private readonly Lazy<ClaimsPrincipal> _user;
    private readonly Lazy<string> _correlationId;
    private readonly Lazy<Guid> _requestId;
    private readonly Lazy<DateTime> _requestStartTimeUtc;
    private readonly Lazy<string> _requestPath;
    private readonly Lazy<string> _httpMethod;
    private readonly Lazy<string?> _clientIpAddress;
    private readonly Lazy<string?> _userAgent;

    public AppContext(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;

        // Lazy初期化の設定
        _userId = new Lazy<Guid>(GetUserId);
        _userName = new Lazy<string>(GetUserName);
        _tenantId = new Lazy<Guid?>(GetTenantId);
        _isAuthenticated = new Lazy<bool>(GetIsAuthenticated);
        _user = new Lazy<ClaimsPrincipal>(GetUser);
        _correlationId = new Lazy<string>(GetCorrelationId);
        _requestId = new Lazy<Guid>(GetRequestId);
        _requestStartTimeUtc = new Lazy<DateTime>(GetRequestStartTimeUtc);
        _requestPath = new Lazy<string>(GetRequestPath);
        _httpMethod = new Lazy<string>(GetHttpMethod);
        _clientIpAddress = new Lazy<string?>(GetClientIpAddress);
        _userAgent = new Lazy<string?>(GetUserAgent);
    }

    // ===================================
    // ユーザー情報
    // ===================================

    public Guid UserId => _userId.Value;

    public string UserName => _userName.Value;

    public Guid? TenantId => _tenantId.Value;

    public bool IsAuthenticated => _isAuthenticated.Value;

    public ClaimsPrincipal User => _user.Value;

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool IsInAnyRole(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            return false;

        return roles.Any(role => IsInRole(role));
    }

    // ===================================
    // リクエスト追跡情報
    // ===================================

    public string CorrelationId => _correlationId.Value;

    public Guid RequestId => _requestId.Value;

    // ===================================
    // リクエストメタデータ
    // ===================================

    public DateTime RequestStartTimeUtc => _requestStartTimeUtc.Value;

    public string RequestPath => _requestPath.Value;

    public string HttpMethod => _httpMethod.Value;

    public string? ClientIpAddress => _clientIpAddress.Value;

    public string? UserAgent => _userAgent.Value;

    // ===================================
    // 環境情報
    // ===================================

    public string EnvironmentName => _environment.EnvironmentName;

    public string HostName => Environment.MachineName;

    public string ApplicationName => _environment.ApplicationName;

    // ===================================
    // Private Methods - Lazy初期化用
    // ===================================

    private Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // 未認証の場合
        return Guid.Empty;
    }

    private string GetUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name
               ?? "Anonymous";
    }

    private Guid? GetTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst("TenantId")?.Value;

        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    private bool GetIsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated
               ?? false;
    }

    private ClaimsPrincipal GetUser()
    {
        return _httpContextAccessor.HttpContext?.User
               ?? new ClaimsPrincipal();
    }

    private string GetCorrelationId()
    {
        const string CorrelationIdKey = "CorrelationId";
        var correlationId = _httpContextAccessor.HttpContext?.Items[CorrelationIdKey] as string;
        return correlationId ?? "N/A";
    }

    private Guid GetRequestId()
    {
        const string RequestIdKey = "RequestId";
        var requestId = _httpContextAccessor.HttpContext?.Items[RequestIdKey];

        if (requestId is Guid guidValue)
        {
            return guidValue;
        }

        // RequestIdが未設定の場合は新規生成（通常は発生しない）
        var newRequestId = Guid.NewGuid();
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items[RequestIdKey] = newRequestId;
        }

        return newRequestId;
    }

    private DateTime GetRequestStartTimeUtc()
    {
        const string RequestStartTimeKey = "RequestStartTime";
        var startTime = _httpContextAccessor.HttpContext?.Items[RequestStartTimeKey];

        if (startTime is DateTime dateTimeValue)
        {
            return dateTimeValue;
        }

        // RequestStartTimeが未設定の場合は現在時刻を返す
        var now = DateTime.UtcNow;
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items[RequestStartTimeKey] = now;
        }

        return now;
    }

    private string GetRequestPath()
    {
        return _httpContextAccessor.HttpContext?.Request.Path.Value
               ?? "/";
    }

    private string GetHttpMethod()
    {
        return _httpContextAccessor.HttpContext?.Request.Method
               ?? "UNKNOWN";
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // X-Forwarded-For ヘッダーをチェック（プロキシ経由の場合）
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        // リモートIPアドレスを取得
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
    }
}
