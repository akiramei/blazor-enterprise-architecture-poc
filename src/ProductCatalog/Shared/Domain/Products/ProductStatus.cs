namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 商品ステータス
///
/// 【パターン: 状態管理】
///
/// 使用シナリオ:
/// - エンティティのライフサイクル管理が必要な場合
/// - 状態に応じて操作の可否を制御したい場合
/// - 状態遷移に明確なルールがある場合
///
/// 状態遷移:
/// Draft (下書き) → Published (公開中) → Archived (アーカイブ済み)
///          ↑______________|
///
/// 実装ガイド:
/// - enumで状態を定義（シンプルで分かりやすい）
/// - 複雑な状態管理が必要な場合はState Patternも検討
/// - 状態遷移のルールは集約ルート（Product）内に実装
///
/// AI実装時の注意:
/// - 状態遷移は必ずドメインメソッド経由で行う（Product.Publish(), Product.Archive()）
/// - 直接ステータスを変更しない（product.Status = ProductStatus.Published は NG）
/// - 状態に応じた操作制限は集約ルート内でチェックする
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// 下書き - 作成後の初期状態
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 公開中 - ユーザーに表示される状態
    /// </summary>
    Published = 1,

    /// <summary>
    /// アーカイブ済み - 非表示だが削除はされていない状態
    /// </summary>
    Archived = 2
}
