using Shared.Application;
using Shared.Application.Interfaces;
using ProductCatalog.Shared.Application.DTOs;

namespace GetProducts.Application;

/// <summary>
/// 商品一覧取得Query
///
/// 【パターン: 一覧取得Query】
///
/// 使用シナリオ:
/// - 全データを取得して表示したい場合
/// - フィルタリングやページングが不要な場合
/// - キャッシュを効かせたい場合
/// - 簡単なリスト表示
///
/// 実装ガイド:
/// - パラメータなし（全件取得）
/// - ICacheableQueryを実装してキャッシュを有効化
/// - キャッシュキーは固定（"products-all"など）
/// - Read Model（Dapper）で最適化
///
/// AI実装時の注意:
/// - IProductReadRepository経由で取得（Dapper）
/// - キャッシュはCachingBehaviorが自動的に処理
/// - DTOで返す（Domainエンティティをそのまま返さない）
/// - 大量データの場合はページングを検討（SearchProductsQueryを使用）
///
/// 関連パターン:
/// - GetProductById: 単一取得の場合
/// - SearchProducts: 検索、フィルタリング、ページングが必要な場合
/// </summary>
public sealed record GetProductsQuery() : IQuery<Result<IEnumerable<ProductDto>>>, ICacheableQuery
{
    /// <summary>
    /// キャッシュキー（全商品一覧は固定キー）
    /// </summary>
    public string GetCacheKey() => "products-all";

    /// <summary>
    /// キャッシュ期間: 5分
    /// （一覧データは比較的短めにキャッシュ）
    /// </summary>
    public int CacheDurationMinutes => 5;
}
