using Microsoft.Extensions.Logging;
using ProductCatalog.Shared.UI.Store;

namespace ProductCatalog.Shared.UI.Actions;

/// <summary>
/// 商品編集画面のUI手順を管理
/// </summary>
public sealed class ProductEditActions
{
    private readonly ProductEditStore _store;
    private readonly ILogger<ProductEditActions> _logger;

    public ProductEditActions(
        ProductEditStore store,
        ILogger<ProductEditActions> logger)
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
            _logger.LogError(ex, "商品データの読み込みに失敗しました: {ProductId}", productId);
        }
    }

    public async Task<bool> SaveAsync(
        Guid productId,
        string name,
        string description,
        decimal price,
        int stock,
        long version,
        CancellationToken ct = default)
    {
        try
        {
            return await _store.SaveAsync(productId, name, description, price, stock, version, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品更新に失敗しました: {ProductId}", productId);
            return false;
        }
    }
}
