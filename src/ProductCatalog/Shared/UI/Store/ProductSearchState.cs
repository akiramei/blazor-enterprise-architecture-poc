using Shared.Application;
using Shared.Application.Common;
using ProductCatalog.Shared.Application.DTOs;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品検索画面の状態（不変）
/// </summary>
public sealed record ProductSearchState
{
    public static readonly ProductSearchState Empty = new();

    public PagedResult<ProductDto>? SearchResult { get; init; }
    public bool IsSearching { get; init; }
    public string? ErrorMessage { get; init; }

    // Search filters
    public string? NameFilter { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? Status { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string OrderBy { get; init; } = "Name";
    public bool IsDescending { get; init; }
}
