using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Application.Products.Queries;

/// <summary>
/// 商品一覧取得Query（キャッシュ対応）
/// </summary>
public sealed record GetProductsQuery() : IQuery<Result<IEnumerable<ProductDto>>>, ICacheableQuery
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
