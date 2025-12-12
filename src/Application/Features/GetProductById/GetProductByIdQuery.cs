using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetProductById;

/// <summary>
/// 商品単一取得Query
/// </summary>
public sealed class GetProductByIdQuery : IQuery<Result<ProductDetailDto>>, ICacheableQuery
{
    public Guid ProductId { get; init; }

    /// <summary>
    /// キャッシュキー
    /// </summary>
    public string GetCacheKey() => $"product_{ProductId}";

    /// <summary>
    /// キャッシュ期間: 10分
    /// </summary>
    public int CacheDurationMinutes => 10;
}
