using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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
    private readonly IHostEnvironment _environment;

    public CachingBehavior(
        IMemoryCache cache,
        ICurrentUserService currentUser,
        ILogger<CachingBehavior<TRequest, TResponse>> logger,
        IHostEnvironment environment)
    {
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
        _environment = environment;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // テスト環境ではキャッシュを無効化（テストの独立性を保つため）
        if (_environment.EnvironmentName == "Test")
        {
            _logger.LogDebug("キャッシュ無効（Test環境）: {RequestType}", typeof(TRequest).Name);
            return await next();
        }

        // CRITICAL: キーに必ずユーザー/テナント情報を含める（キャッシュ誤配信防止）
        var userSegment = _currentUser.UserId.ToString("N");
        var tenantSegment = _currentUser.TenantId?.ToString("N") ?? "default";
        var requestSegment = request.GetCacheKey();

        var cacheKey = $"{typeof(TRequest).Name}:{tenantSegment}:{userSegment}:{requestSegment}";

        // キャッシュから取得
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
        {
            _logger.LogDebug("【キャッシュヒット】 Key={CacheKey}, Data={@CachedData}",
                cacheKey, cached);
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
