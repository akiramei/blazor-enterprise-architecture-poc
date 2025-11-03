using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Application.Features.Products.DeleteProduct;

/// <summary>
/// 商品削除Command
///
/// 【パターン: 削除系Command】
///
/// 使用シナリオ:
/// - 単一のエンティティを削除したい場合
/// - 削除前にビジネスルールを検証したい場合（在庫チェックなど）
/// - 論理削除を実装する場合
///
/// 実装ガイド:
/// - IDのみをパラメータに持つ（シンプル）
/// - 物理削除ではなく論理削除を推奨（IsDeleted = true）
/// - 冪等性キーを含めて重複実行を防止
/// - 削除前のビジネスルール検証はDomain層（Entity.Delete()）で行う
///
/// AI実装時の注意:
/// - Handler内でEntity.Delete()メソッドを呼ぶ（直接削除しない）
/// - ビジネスルール検証はDomain層に委譲
/// - Repository.DeleteAsync() ではなく、Entity.Delete() → Repository.SaveAsync()
/// - 論理削除の場合は、IsDeletedフラグを立てるだけ
/// - 削除後はドメインイベントを発行（統合イベント配信、通知などに使用）
///
/// 物理削除 vs 論理削除:
/// - 物理削除: DBから完全に削除（取り消し不可、監査ログがない）
/// - 論理削除: IsDeletedフラグを立てるだけ（取り消し可能、監査ログ残る）← 推奨
///
/// 関連パターン:
/// - BulkDeleteProducts: 複数削除の場合
/// </summary>
public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result>
{
    /// <summary>
    /// 冪等性キー（重複実行防止）
    ///
    /// IdempotencyBehaviorがこのプロパティを検出し、
    /// 同じキーでの実行は1回のみ処理される
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
