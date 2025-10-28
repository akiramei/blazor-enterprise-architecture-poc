using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Application.Features.Products.SearchProducts;

/// <summary>
/// 商品検索Handler
///
/// 【パターン: 検索Handler】
///
/// 処理フロー:
/// 1. IProductReadRepositoryを使用（Dapperで最適化）
/// 2. 検索条件を構築
/// 3. ページング処理
/// 4. 総件数を取得
/// 5. PagedResultに変換
/// 6. 結果を返す
///
/// 実装ガイド:
/// - 参照系なのでRead Model（Dapper）を使用して最適化
/// - 動的SQL構築時はSQLインジェクション対策を徹底
/// - ページング処理はDBレベルで実行（OFFSET/FETCH）
/// - 総件数取得は効率的に（COUNT(*) OVER() または別クエリ）
///
/// AI実装時の注意:
/// - IProductReadRepositoryはRead専用（書き込みは不可）
/// - フィルタ条件がnullの場合は、WHERE句に含めない
/// - ソート項目はホワイトリスト方式で検証（SQLインジェクション対策）
/// - ページングパラメータの妥当性チェック（Page >= 1, PageSize > 0）
/// - エラーハンドリングを適切に
///
/// 最適化ポイント:
/// - インデックスが効くようなクエリを構築
/// - 不要なJOINを避ける
/// - 総件数取得を最適化（必要な場合のみ）
/// </summary>
public sealed class SearchProductsHandler
    : IRequestHandler<SearchProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductReadRepository _readRepository;
    private readonly ILogger<SearchProductsHandler> _logger;

    public SearchProductsHandler(
        IProductReadRepository readRepository,
        ILogger<SearchProductsHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        // パラメータ検証
        if (query.Page < 1)
        {
            return Result.Fail<PagedResult<ProductDto>>("ページ番号は1以上である必要があります");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return Result.Fail<PagedResult<ProductDto>>("ページサイズは1以上100以下である必要があります");
        }

        // ソート項目のホワイトリスト検証（SQLインジェクション対策）
        var allowedSortColumns = new[] { "Name", "Price", "Stock", "Status" };
        if (!allowedSortColumns.Contains(query.OrderBy, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("無効なソート項目: {OrderBy}", query.OrderBy);
            return Result.Fail<PagedResult<ProductDto>>($"無効なソート項目です: {query.OrderBy}");
        }

        try
        {
            // IProductReadRepository経由で検索（Dapperで実装）
            var result = await _readRepository.SearchAsync(
                nameFilter: query.NameFilter,
                minPrice: query.MinPrice,
                maxPrice: query.MaxPrice,
                status: query.Status,
                page: query.Page,
                pageSize: query.PageSize,
                orderBy: query.OrderBy,
                isDescending: query.IsDescending,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "商品検索を実行しました。条件: Name={Name}, MinPrice={MinPrice}, MaxPrice={MaxPrice}, " +
                "Status={Status}, Page={Page}, PageSize={PageSize}, 結果件数: {Count}/{Total}",
                query.NameFilter,
                query.MinPrice,
                query.MaxPrice,
                query.Status,
                query.Page,
                query.PageSize,
                result.Items.Count,
                result.TotalCount);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品検索中にエラーが発生しました");
            return Result.Fail<PagedResult<ProductDto>>("商品検索中にエラーが発生しました");
        }
    }
}
