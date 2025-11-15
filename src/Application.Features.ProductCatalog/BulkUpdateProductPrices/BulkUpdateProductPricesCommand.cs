using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;

namespace Application.Features.ProductCatalog.BulkUpdateProductPrices;

/// <summary>
/// 商品価格一括更新コマンド
///
/// 【パターン: バルク更新 - Command】
///
/// 使用シナリオ:
/// - 複数商品の価格を一括で変更
/// - セール価格の一括設定
/// - 価格改定の一括適用
///
/// 実装ガイド:
/// - ProductId と 新価格のペアを受け取る
/// - 各商品のビジネスルール検証（50%以上値下げ制限など）
/// - 部分成功/失敗の追跡
///
/// AI実装時の注意:
/// - 楽観的同時実行制御のため Version も受け取る
/// - 各商品ごとにドメインルールを検証
/// - エラーが発生した商品は記録するが、他の商品は処理継続
/// - 最大更新件数制限（例: 100件）
/// </summary>
public record BulkUpdateProductPricesCommand : ICommand<Result<BulkOperationResult>>
{
    public IReadOnlyList<ProductPriceUpdate> Updates { get; init; } = Array.Empty<ProductPriceUpdate>();
}

/// <summary>
/// 商品価格更新情報
/// </summary>
public record ProductPriceUpdate(
    Guid ProductId,
    decimal NewPrice,
    int Version
);
