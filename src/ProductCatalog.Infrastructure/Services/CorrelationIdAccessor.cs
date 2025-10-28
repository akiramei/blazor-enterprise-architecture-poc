using Microsoft.AspNetCore.Http;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Infrastructure.Services;

/// <summary>
/// HttpContext から CorrelationID を取得するサービス
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private const string CorrelationIdKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId
    {
        get
        {
            var correlationId = _httpContextAccessor.HttpContext?.Items[CorrelationIdKey] as string;
            return correlationId ?? "N/A";
        }
    }
}
