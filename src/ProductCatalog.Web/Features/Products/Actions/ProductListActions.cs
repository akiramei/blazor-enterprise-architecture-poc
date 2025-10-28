using ProductCatalog.Web.Features.Products.Store;

namespace ProductCatalog.Web.Features.Products.Actions;

/// <summary>
/// 商品一覧画面のUI手順を管理
/// </summary>
public sealed class ProductListActions
{
    private readonly ProductsStore _store;
    private readonly ILogger<ProductListActions> _logger;

    public ProductListActions(
        ProductsStore store,
        ILogger<ProductListActions> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            await _store.LoadAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品一覧の読み込みに失敗しました");
        }
    }

    public async Task DeleteAsync(Guid productId, CancellationToken ct = default)
    {
        // Note: In a real application, you would show a confirmation dialog
        var success = await _store.DeleteAsync(productId, ct);

        if (success)
        {
            _logger.LogInformation("商品を削除しました: {ProductId}", productId);
        }
        else
        {
            _logger.LogWarning("商品削除に失敗しました: {ProductId}", productId);
        }
    }
}
