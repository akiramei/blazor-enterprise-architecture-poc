using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Application.Features.Products.GetProductById;

/// <summary>
/// 商品単一取得Query
///
/// 【パターン: 単一取得Query】
///
/// 使用シナリオ:
/// - 詳細画面で単一のエンティティを表示する場合
/// - 編集画面でデータをロードする場合
/// - IDが分かっている特定のエンティティを取得する場合
///
/// 実装ガイド:
/// - IDのみをパラメータに持つ（シンプル）
/// - ICacheableQueryを実装してキャッシュを有効化
/// - キャッシュキーは「エンティティ名_ID」形式
/// - 存在しない場合はnullまたはFailureを返す
///
/// AI実装時の注意:
/// - Repository経由で集約全体を取得（子エンティティも含む）
/// - DTOに変換して返す（Domain層のエンティティをそのまま返さない）
/// - キャッシュ期間は適切に設定（頻繁に変更されるデータは短く）
///
/// 関連パターン:
/// - GetProducts: 一覧取得の場合
/// - SearchProducts: 条件検索の場合
/// - UpdateProduct: 編集画面での初期データ取得に使用
/// </summary>
public sealed record GetProductByIdQuery(Guid ProductId)
    : IQuery<Result<ProductDetailDto>>, ICacheableQuery
{
    /// <summary>
    /// キャッシュキー（商品IDをキーに含める）
    /// </summary>
    public string GetCacheKey() => $"product_{ProductId}";

    /// <summary>
    /// キャッシュ期間: 10分
    /// （詳細データは比較的長くキャッシュ可能）
    /// </summary>
    public int CacheDurationMinutes => 10;
}
