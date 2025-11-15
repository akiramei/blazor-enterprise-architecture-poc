using Application.Core.Queries;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;

namespace Application.Features.ProductCatalog.GetProductById;

/// <summary>
/// 商品単一取得クエリハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. Read Repositoryから商品詳細DTOを取得
/// 2. 存在チェック
/// 3. 結果を返す
///
/// 【リファクタリング成果】
/// - Before: 約65行 (ログ含む)
/// - After: 約30行 (クエリロジックのみ)
/// - 削減率: 54%
/// </summary>
public class GetProductByIdQueryHandler
    : QueryPipeline<GetProductByIdQuery, ProductDetailDto>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductByIdQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    protected override async Task<Result<ProductDetailDto>> ExecuteAsync(
        GetProductByIdQuery query,
        CancellationToken ct)
    {
        // Read Repository経由でDTOを直接取得
        var productDto = await _readRepository.GetByIdAsync(query.ProductId, ct);

        if (productDto is null)
            return Result.Fail<ProductDetailDto>("商品が見つかりません");

        return Result.Success(productDto);
    }
}
