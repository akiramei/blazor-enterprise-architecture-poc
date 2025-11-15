using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using ProductCatalog.Shared.Application.DTOs;
using Domain.ProductCatalog.Products;

namespace SearchProducts.Application;

/// <summary>
/// 商品検索Query
///
/// 【パターン: 検索・フィルタリング・ページング】
///
/// 使用シナリオ:
/// - ユーザーが条件を指定してデータを検索する場合
/// - 大量データをページング表示する場合
/// - 複数の条件でフィルタリングする場合
/// - ソート順を指定できるようにしたい場合
///
/// 実装ガイド:
/// - フィルタ条件はnull許容で、nullの場合は条件なし（柔軟性）
/// - ページング: Page（1始まり）、PageSize
/// - ソート: OrderBy（フィールド名）、IsDescending（降順フラグ）
/// - キャッシュキーには全パラメータを含める（検索条件ごとにキャッシュ）
///
/// AI実装時の注意:
/// - フィルタ条件が増えるとキャッシュキーも複雑になる
/// - 動的SQLを使用する場合はSQLインジェクション対策を徹底
/// - Dapper使用時はパラメータ化クエリを使用
/// - ページング処理は効率的に（OFFSET/FETCH または ROW_NUMBER）
/// - 件数が多い場合は総件数取得を最適化
///
/// 関連パターン:
/// - GetProducts: シンプルな全件取得
/// - GetProductById: 単一取得
/// </summary>
public sealed record SearchProductsQuery(
    string? NameFilter = null,       // 名前で部分一致検索（null = 条件なし）
    decimal? MinPrice = null,        // 最低価格（null = 条件なし）
    decimal? MaxPrice = null,        // 最高価格（null = 条件なし）
    ProductStatus? Status = null,    // ステータス（null = 条件なし）
    int Page = 1,                    // ページ番号（1始まり）
    int PageSize = 20,               // ページサイズ（デフォルト20件）
    string OrderBy = "Name",         // ソート項目（デフォルト: 名前順）
    bool IsDescending = false        // 降順フラグ（デフォルト: 昇順）
) : IQuery<Result<PagedResult<ProductDto>>>, ICacheableQuery
{
    /// <summary>
    /// キャッシュキー（全パラメータを含める）
    /// </summary>
    public string GetCacheKey() =>
        $"products_search_{NameFilter}_{MinPrice}_{MaxPrice}_{Status}_{Page}_{PageSize}_{OrderBy}_{IsDescending}";

    /// <summary>
    /// キャッシュ期間: 5分
    /// （検索結果は比較的短めにキャッシュ）
    /// </summary>
    public int CacheDurationMinutes => 5;
}
