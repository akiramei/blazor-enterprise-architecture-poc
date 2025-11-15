using Domain.ProductCatalog.Products;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;

namespace Application.Features.SearchProducts;

/// <summary>
/// 商品検索Query
/// </summary>
public sealed record SearchProductsQuery(
    string? NameFilter = null,       // 名前で部分一致検索（null = 条件なし）
    decimal? MinPrice = null,        // 最低価格（null = 条件なし）
    decimal? MaxPrice = null,        // 最高価格（null = 条件なし）
    ProductStatus? Status = null,    // ステータス（null = 条件なし）
    int Page = 1,                    // ページ番号（1始まり）
    int PageSize = 20,               // ページサイズ（デフォルト20件）
    string OrderBy = "Name",         // ソート項目（デフォルト: 名前順）
    bool IsDescending = false        // 降順フラグ（デフォルト: 昇順）
) : IQuery<Result<PagedResult<ProductDto>>>, ICacheableQuery
{
    /// <summary>
    /// キャッシュキー（全パラメータを含める）
    /// </summary>
    public string GetCacheKey() =>
        $"products_search_{NameFilter}_{MinPrice}_{MaxPrice}_{Status}_{Page}_{PageSize}_{OrderBy}_{IsDescending}";

    /// <summary>
    /// キャッシュ期間: 5分
    /// （検索結果は比較的短めにキャッシュ）
    /// </summary>
    public int CacheDurationMinutes => 5;
}
