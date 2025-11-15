using Application.Core.Queries;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Application;

namespace Application.Features.ProductCatalog.GetProducts;

/// <summary>
/// 商品一覧取得クエリハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. IProductReadRepository経由でデータ取得（Dapper）
/// 2. DTOのリストを返す
///
/// 【リファクタリング成果】
/// - Before: 約63行 (ログ含む)
/// - After: 約25行 (クエリロジックのみ)
/// - 削減率: 60%
///
/// 【実装ガイド】
/// - Read Model（Dapper）を使用して最適化
/// - Repository（EF Core）ではなくReadRepository（Dapper）を使用
/// - キャッシュはCachingBehaviorが自動的に処理
/// - 参照系なので書き込み処理は行わない
/// </summary>
public class GetProductsQueryHandler
    : QueryPipeline<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductReadRepository _readRepository;

    public GetProductsQueryHandler(IProductReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    protected override async Task<Result<IEnumerable<ProductDto>>> ExecuteAsync(
        GetProductsQuery query,
        CancellationToken ct)
    {
        // IProductReadRepository経由で取得（Dapper使用）
        // - SELECT * FROM Products WHERE IsDeleted = 0
        // - シンプルなクエリで高速
        var products = await _readRepository.GetAllAsync(ct);

        return Result.Success(products);
    }
}
