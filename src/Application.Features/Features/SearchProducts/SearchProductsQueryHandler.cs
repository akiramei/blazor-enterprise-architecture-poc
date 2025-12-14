using Application.Core.Queries;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;
using Shared.Application.Common;

namespace Application.Features.SearchProducts;

/// <summary>
/// 商品検索クエリハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. パラメータ検証（ページング、ソート項目）
/// 2. Read Repositoryで検索実行（Dapper最適化）
/// 3. ページング結果を返す
///
/// 【リファクタリング成果】
/// - Before: 約114行 (try-catch, ログ含む)
/// - After: 約70行 (検索ロジックのみ)
/// - 削減率: 39%
/// </summary>
public sealed class SearchProductsQueryHandler
    : QueryPipeline<SearchProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductReadRepository _readRepository;

    // ソート項目のホワイトリスト（SQLインジェクション対策）
    private static readonly string[] AllowedSortColumns = { "Name", "Price", "Stock", "Status" };

    public SearchProductsQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    protected override async Task<Result<PagedResult<ProductDto>>> ExecuteAsync(
        SearchProductsQuery query,
        CancellationToken ct)
    {
        // 1. パラメータ検証
        var validationResult = ValidateParameters(query);
        if (!validationResult.IsSuccess)
            return validationResult;

        // 2. IProductReadRepository経由で検索（Dapperで実装）
        var result = await _readRepository.SearchAsync(
            nameFilter: query.NameFilter,
            minPrice: query.MinPrice,
            maxPrice: query.MaxPrice,
            status: query.Status,
            page: query.Page,
            pageSize: query.PageSize,
            orderBy: query.OrderBy,
            isDescending: query.IsDescending,
            cancellationToken: ct);

        return Result.Success(result);
    }

    private static Result<PagedResult<ProductDto>> ValidateParameters(SearchProductsQuery query)
    {
        // ページ番号検証
        if (query.Page < 1)
            return Result.Fail<PagedResult<ProductDto>>("ページ番号は1以上である必要があります");

        // ページサイズ検証
        if (query.PageSize < 1 || query.PageSize > 100)
            return Result.Fail<PagedResult<ProductDto>>("ページサイズは1以上100以下である必要があります");

        // ソート項目のホワイトリスト検証（SQLインジェクション対策）
        if (!AllowedSortColumns.Contains(query.OrderBy, StringComparer.OrdinalIgnoreCase))
            return Result.Fail<PagedResult<ProductDto>>($"無効なソート項目です: {query.OrderBy}");

        return Result.Success<PagedResult<ProductDto>>(null!);
    }
}
