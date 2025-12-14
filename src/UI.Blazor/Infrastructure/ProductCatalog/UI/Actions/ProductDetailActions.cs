using Microsoft.Extensions.Logging;
using ProductCatalog.Shared.UI.Store;

namespace ProductCatalog.Shared.UI.Actions;

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
        _logger.LogInformation("[Actions] LoadAsync started for ProductId: {ProductId}", productId);
        try
        {
            _logger.LogInformation("[Actions] Calling Store.LoadAsync");
            await _store.LoadAsync(productId, ct);
            _logger.LogInformation("[Actions] Store.LoadAsync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Actions] 商品詳細の読み込みに失敗しました: {ProductId}", productId);
        }
    }
}
