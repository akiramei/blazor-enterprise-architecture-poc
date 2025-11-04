using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;

namespace BulkDeleteProducts.Application;

/// <summary>
/// 商品一括削除Command
///
/// 【パターン: 一括処理Command】
///
/// 使用シナリオ:
/// - 複数のエンティティを一度に削除したい場合
/// - UI上でチェックボックスで複数選択して削除する場合
/// - 管理画面でバッチ操作を実行する場合
///
/// 実装ガイド:
/// - 複数のIDをコレクションで受け取る
/// - 各削除は個別にビジネスルール検証を行う（ループ内でEntity.Delete()）
/// - 1つでも失敗したら全体をロールバック（トランザクション）
/// - 成功した件数と失敗した件数を返す（BulkOperationResult）
/// - 冪等性キーを含める（重複実行防止）
///
/// AI実装時の注意:
/// - ループ内で個別にEntity.Delete()を呼ぶ（ビジネスルールを通す）
/// - 一括削除でも必ずビジネスルールを検証する（在庫チェックなど）
/// - パフォーマンスが気になる場合は、Read Modelで事前チェックを検討
/// - トランザクションは TransactionBehavior が自動的に管理
/// - 大量データの場合は、バッチサイズを制限する
///
/// 関連パターン:
/// - DeleteProduct: 単一削除の場合
/// </summary>
public sealed record BulkDeleteProductsCommand(
    IEnumerable<Guid> ProductIds
) : ICommand<Result<BulkOperationResult>>
{
    /// <summary>
    /// 冪等性キー（重複実行防止）
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
