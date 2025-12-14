using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetProducts;

/// <summary>
/// 商品一覧取得Query
/// </summary>
public sealed class GetProductsQuery : IQuery<Result<IEnumerable<ProductDto>>>, ICacheableQuery
{
    /// <summary>
    /// キャッシュキー（全商品一覧は固定キー）
    /// </summary>
    public string GetCacheKey() => "products-all";

    /// <summary>
    /// キャッシュ期間: 5分
    /// </summary>
    public int CacheDurationMinutes => 5;
}
