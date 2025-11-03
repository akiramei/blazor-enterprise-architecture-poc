using ProductCatalog.Web.Features.Products.Store;

namespace ProductCatalog.Web.Features.Products.Actions;

/// <summary>
/// 商品詳細画面のUI手順を管理
/// </summary>
public sealed class ProductDetailActions
{
    private readonly ProductDetailStore _store;
    private readonly ILogger<ProductDetailActions> _logger;

    public ProductDetailActions(
        ProductDetailStore store,
        ILogger<ProductDetailActions> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task LoadAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            await _store.LoadAsync(productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品詳細の読み込みに失敗しました: {ProductId}", productId);
        }
    }
}
