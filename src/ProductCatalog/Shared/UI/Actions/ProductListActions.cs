using Microsoft.Extensions.Logging;
using MediatR;
using Shared.Application;
using Shared.Application.Common;
using BulkDeleteProducts.Application;
using ImportProductsFromCsv.Application;
using ProductCatalog.Shared.UI.Store;

namespace ProductCatalog.Shared.UI.Actions;

/// <summary>
/// 商品一覧画面のUI手順を管理
/// </summary>
public sealed class ProductListActions
{
    private readonly ProductsStore _store;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductListActions> _logger;

    public ProductListActions(
        ProductsStore store,
        IMediator mediator,
        ILogger<ProductListActions> logger)
    {
        _store = store;
        _mediator = mediator;
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

    /// <summary>
    /// CSVインポート
    ///
    /// 【パターン: CSVインポート - Action】
    ///
    /// 使用シナリオ:
    /// - CSVファイルから商品データを一括登録
    /// - バリデーションエラーをユーザーに報告
    ///
    /// 実装ガイド:
    /// - MediatR経由でImportProductsFromCsvCommandを実行
    /// - 成功後は商品一覧を再読み込み
    /// - エラーがあれば詳細を返す
    /// </summary>
    public async Task<BulkOperationResult?> ImportFromCsvAsync(Stream csvStream, CancellationToken ct = default)
    {
        try
        {
            var command = new ImportProductsFromCsvCommand(csvStream);
            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("CSVインポートに失敗しました: {Error}", result.Error);
                return null;
            }

            // インポート成功後、商品一覧を再読み込み
            if (result.Value!.SucceededCount > 0)
            {
                await LoadAsync(ct);
            }

            _logger.LogInformation(
                "CSVインポートを実行しました: 成功={SucceededCount}, 失敗={FailedCount}, 合計={TotalCount}",
                result.Value.SucceededCount,
                result.Value.FailedCount,
                result.Value.TotalCount);

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSVインポート中にエラーが発生しました");
            return null;
        }
    }
}
