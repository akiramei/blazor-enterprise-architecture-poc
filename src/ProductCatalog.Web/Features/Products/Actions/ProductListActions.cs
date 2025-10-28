using ProductCatalog.Application.Common;
using ProductCatalog.Application.Features.Products.BulkDeleteProducts;
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

    public async Task<BulkOperationResult> BulkDeleteAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        try
        {
            var result = await _store.BulkDeleteAsync(productIds, ct);

            _logger.LogInformation(
                "一括削除を実行しました: 成功={SucceededCount}, 失敗={FailedCount}, 合計={TotalCount}",
                result.SucceededCount,
                result.FailedCount,
                result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "一括削除処理に失敗しました");
            return BulkOperationResult.AllFailed(
                productIds.Count(),
                new[] { ex.Message });
        }
    }
}
