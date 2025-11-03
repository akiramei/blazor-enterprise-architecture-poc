using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Application.Features.Products.GetProducts;

/// <summary>
/// 商品一覧取得Handler
///
/// 【パターン: 一覧取得Handler】
///
/// 処理フロー:
/// 1. IProductReadRepository経由でデータ取得（Dapper）
/// 2. DTOのリストを返す
///
/// 実装ガイド:
/// - Read Model（Dapper）を使用して最適化
/// - Repository（EF Core）ではなくReadRepository（Dapper）を使用
/// - キャッシュはCachingBehaviorが自動的に処理
/// - 参照系なので書き込み処理は行わない
///
/// AI実装時の注意:
/// - IProductReadRepository経由で取得（読み取り専用）
/// - 複雑なJOINやフィルタリングが必要な場合はDapperが有効
/// - キャッシュキーが同じ場合、Behaviorが結果をキャッシュ
/// - 大量データの場合はページング必須（SearchProductsQueryを使用）
///
/// Read Model（Dapper） vs Repository（EF Core）:
/// ✅ Read Model: 参照系クエリに最適（高速、柔軟なSQL）
/// ❌ Repository: 更新系に使用（集約の整合性を保つ）
/// </summary>
public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<IEnumerable<ProductDto>>>
{
    private readonly IProductReadRepository _readRepository;
    private readonly ILogger<GetProductsHandler> _logger;

    public GetProductsHandler(
        IProductReadRepository readRepository,
        ILogger<GetProductsHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ProductDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("商品一覧を取得しています");

        // IProductReadRepository経由で取得（Dapper使用）
        // - SELECT * FROM Products WHERE IsDeleted = 0
        // - シンプルなクエリで高速
        var products = await _readRepository.GetAllAsync(cancellationToken);

        _logger.LogInformation("商品一覧を取得しました: {Count}件", products.Count());

        return Result.Success(products);
    }
}
