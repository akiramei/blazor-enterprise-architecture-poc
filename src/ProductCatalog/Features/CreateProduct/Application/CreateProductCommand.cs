using Shared.Application;
using Shared.Application.Interfaces;

namespace CreateProduct.Application;

/// <summary>
/// 商品作成Command
///
/// 【パターン: 作成系Command】
///
/// 使用シナリオ:
/// - 新しいエンティティを作成する場合
/// - 登録フォームからの保存処理
/// - 初期データのセットアップ
///
/// 実装ガイド:
/// - 作成に必要な最小限の情報をパラメータに含める
/// - 自動生成される値（ID、作成日時など）はパラメータに含めない
/// - 戻り値は作成されたエンティティのIDを返す（Result<Guid>）
/// - 冪等性キーを含めて重複実行を防止
///
/// AI実装時の注意:
/// - ファクトリメソッド（Entity.Create()）経由で作成
/// - ビジネスルールはDomain層のファクトリメソッド内で検証
/// - Handler内では取得・保存のオーケストレーションのみ
/// - 作成されたIDを返すことで、後続処理（詳細画面への遷移など）を可能にする
///
/// 関連パターン:
/// - UpdateProduct: 作成後の編集
/// - GetProductById: 作成後の確認表示
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock
) : ICommand<Result<Guid>>
{
    /// <summary>
    /// 冪等性キー（重複実行防止）
    ///
    /// IdempotencyBehaviorがこのプロパティを検出し、
    /// 同じキーでの実行は1回のみ処理される
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
