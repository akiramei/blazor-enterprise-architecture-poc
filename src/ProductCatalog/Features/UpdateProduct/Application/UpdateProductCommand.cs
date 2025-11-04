using Shared.Application;
using Shared.Application.Interfaces;

namespace UpdateProduct.Application;

/// <summary>
/// 商品更新Command
///
/// 【パターン: 更新系Command】
///
/// 使用シナリオ:
/// - 既存データの部分的または全体的な変更が必要な場合
/// - 編集画面からの保存処理
/// - 楽観的排他制御が必要な場合（複数ユーザーが同時編集する可能性がある）
///
/// 実装ガイド:
/// - 必ずVersionフィールドを含めて楽観的排他制御を実装
/// - 部分更新の場合は、変更するフィールドのみをパラメータに含める
/// - 冪等性キーを含めて重複実行を防止
/// - IDは変更不可（識別子は不変）
///
/// AI実装時の注意:
/// - Handler内でEntity.ChangeXxx()メソッドを呼ぶ（直接フィールド変更しない）
/// - Versionチェックを最初に実行する
/// - 変更がない場合は早期リターン（最適化）
/// - 楽観的排他制御の競合時は、ユーザーに最新データを取得するよう促す
///
/// 関連パターン:
/// - CreateProduct: 新規作成の場合
/// - GetProductById: 編集画面での初期データ取得
/// </summary>
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    long Version  // 楽観的排他制御用のバージョン番号
) : ICommand<Result>
{
    /// <summary>
    /// 冪等性キー（重複実行防止）
    ///
    /// IdempotencyBehaviorがこのプロパティを検出し、
    /// 同じキーでの実行は1回のみ処理される
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
