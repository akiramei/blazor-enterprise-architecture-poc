using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// キャッシュのPipeline Behavior（Query専用）
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, ICacheableQuery
{
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        IMemoryCache cache,
        ICurrentUserService currentUser,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // CRITICAL: キーに必ずユーザー/テナント情報を含める（キャッシュ誤配信防止）
        var userSegment = _currentUser.UserId.ToString("N");
        var tenantSegment = _currentUser.TenantId?.ToString("N") ?? "default";
        var requestSegment = request.GetCacheKey();

        var cacheKey = $"{typeof(TRequest).Name}:{tenantSegment}:{userSegment}:{requestSegment}";

        // キャッシュから取得
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
        {
            _logger.LogDebug("キャッシュヒット: {CacheKey}", cacheKey);
            return cached!;
        }

        // キャッシュミス: Queryを実行
        _logger.LogDebug("キャッシュミス: {CacheKey}", cacheKey);
        var response = await next();

        // キャッシュに保存
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(request.CacheDurationMinutes)
        };

        _cache.Set(cacheKey, response, cacheOptions);

        _logger.LogDebug(
            "キャッシュ保存: {CacheKey} (有効期限: {Minutes}分)",
            cacheKey,
            request.CacheDurationMinutes);

        return response;
    }
}
