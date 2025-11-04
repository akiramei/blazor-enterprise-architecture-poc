using Microsoft.Extensions.Logging;
using MediatR;
using ExportProductsToCsv.Application;
using ProductCatalog.Shared.Domain.Products;
using ProductCatalog.Shared.UI.Store;

namespace ProductCatalog.Shared.UI.Actions;

/// <summary>
/// 商品検索画面のUI手順を管理
/// </summary>
public sealed class ProductSearchActions
{
    private readonly ProductSearchStore _store;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductSearchActions> _logger;

    public ProductSearchActions(
        ProductSearchStore store,
        IMediator mediator,
        ILogger<ProductSearchActions> logger)
    {
        _store = store;
        _mediator = mediator;
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

    /// <summary>
    /// CSVエクスポート
    ///
    /// 【パターン: CSVエクスポート - Action】
    ///
    /// 使用シナリオ:
    /// - 検索条件に一致する商品をCSV形式でダウンロード
    /// - ファイルダウンロードとして提供
    ///
    /// 実装ガイド:
    /// - MediatR経由でExportProductsToCsvQueryを実行
    /// - 戻り値はbyte[]（CSVファイルのバイナリ）
    /// - エラーハンドリングを行い、失敗時はnullを返す
    /// </summary>
    public async Task<byte[]?> ExportToCsvAsync(
        string? nameFilter = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? status = null,
        CancellationToken ct = default)
    {
        try
        {
            var productStatus = status.HasValue ? (ProductStatus)status.Value : (ProductStatus?)null;

            var query = new ExportProductsToCsvQuery(
                nameFilter,
                minPrice,
                maxPrice,
                productStatus);

            var result = await _mediator.Send(query, ct);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("CSVエクスポートに失敗しました: {Error}", result.Error);
                return null;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSVエクスポート中にエラーが発生しました");
            return null;
        }
    }
}
