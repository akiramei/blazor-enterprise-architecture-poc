using ProductCatalog.Web.Features.Products.Store;

namespace ProductCatalog.Web.Features.Products.Actions;

/// <summary>
/// 商品検索画面のUI手順を管理
/// </summary>
public sealed class ProductSearchActions
{
    private readonly ProductSearchStore _store;
    private readonly ILogger<ProductSearchActions> _logger;

    public ProductSearchActions(
        ProductSearchStore store,
        ILogger<ProductSearchActions> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task SearchAsync(
        string? nameFilter = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? status = null,
        int page = 1,
        int pageSize = 10,
        string orderBy = "Name",
        bool isDescending = false,
        CancellationToken ct = default)
    {
        try
        {
            await _store.SearchAsync(
                nameFilter,
                minPrice,
                maxPrice,
                status,
                page,
                pageSize,
                orderBy,
                isDescending,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品検索に失敗しました");
        }
    }
}
