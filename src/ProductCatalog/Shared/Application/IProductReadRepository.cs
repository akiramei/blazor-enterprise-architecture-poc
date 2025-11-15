using ProductCatalog.Shared.Application.DTOs;
using Domain.ProductCatalog.Products;
using Shared.Application.Common;

namespace ProductCatalog.Shared.Application;

/// <summary>
/// 商品読み取り専用リポジトリインターフェース
///
/// 【パターン: Read Repository（Dapper）】
///
/// 責務:
/// - 参照系のクエリを高速に実行
/// - 複雑なJOINやフィルタリングに対応
/// - DTOへの直接マッピング（Domain層を経由しない）
///
/// 実装ガイド:
/// - Dapperを使用して最適化されたSQLを実行
/// - 更新系の操作は含めない（Read Onlyに特化）
/// - EF Coreではなく生SQLで柔軟に対応
/// - DTO形式で返す（Domainエンティティは返さない）
///
/// AI実装時の注意:
/// - IProductRepository（EF Core）とは用途が異なる
/// - 参照系にはこちらを使用（高速）
/// - 更新系にはIProductRepositoryを使用（整合性保証）
/// - SQLインジェクション対策としてパラメータ化クエリを使用
/// </summary>
public interface IProductReadRepository
{
    /// <summary>
    /// 全商品を取得（論理削除済みを除く）
    /// </summary>
    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDで商品詳細を取得（画像含む）
    /// </summary>
    Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 商品を検索（フィルタリング・ページング・ソート）
    /// </summary>
    Task<PagedResult<ProductDto>> SearchAsync(
        string? nameFilter,
        decimal? minPrice,
        decimal? maxPrice,
        ProductStatus? status,
        int page,
        int pageSize,
        string orderBy,
        bool isDescending,
        CancellationToken cancellationToken = default);
}
