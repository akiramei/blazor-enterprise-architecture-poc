using Shared.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using ProductCatalog.Shared.Domain.Products;

namespace BulkUpdateProductPrices.Application;

/// <summary>
/// 商品価格一括更新ハンドラ
///
/// 【パターン: バルク更新 - Handler】
///
/// 使用シナリオ:
/// - 複数商品の価格を一括で更新
/// - 各商品のビジネスルール検証
/// - 部分成功/失敗の追跡
///
/// 実装ガイド:
/// - 各商品を取得してChangePriceメソッドで価格変更
/// - ドメインエラー（50%以上値下げなど）をキャッチ
/// - 成功した商品のみを保存
/// - 楽観的同時実行制御（Version不一致）を処理
///
/// AI実装時の注意:
/// - トランザクションは使用しない（各商品独立）
/// - エラーメッセージに商品IDを含める
/// - 最大更新件数（100件）チェック
/// - 成功後に通知サービスを呼ぶ
/// </summary>
public class BulkUpdateProductPricesHandler : IRequestHandler<BulkUpdateProductPricesCommand, Result<BulkOperationResult>>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ILogger<BulkUpdateProductPricesHandler> _logger;
    private const int MaxUpdateCount = 100;

    public BulkUpdateProductPricesHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ILogger<BulkUpdateProductPricesHandler> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<BulkOperationResult>> Handle(BulkUpdateProductPricesCommand request, CancellationToken cancellationToken)
    {
        // バリデーション
        if (!request.Updates.Any())
        {
            return Result.Fail<BulkOperationResult>("更新対象の商品が指定されていません");
        }

        if (request.Updates.Count > MaxUpdateCount)
        {
            return Result.Fail<BulkOperationResult>($"一括更新は{MaxUpdateCount}件までです");
        }

        var succeededCount = 0;
        var errors = new List<string>();

        // 各商品を更新
        foreach (var update in request.Updates)
        {
            try
            {
                var product = await _repository.GetAsync(new ProductId(update.ProductId), cancellationToken);

                if (product == null)
                {
                    errors.Add($"商品ID {update.ProductId}: 見つかりません");
                    continue;
                }

                // Versionチェック（楽観的同時実行制御）
                if (product.Version != update.Version)
                {
                    errors.Add($"商品ID {update.ProductId}: 他のユーザーによって更新されています（Version不一致）");
                    continue;
                }

                // 価格変更
                var money = new Money(update.NewPrice, product.Price.Currency);
                product.ChangePrice(money);

                // 保存
                await _repository.SaveAsync(product, cancellationToken);
                succeededCount++;

                _logger.LogInformation("商品価格を更新しました: ProductId={ProductId}, NewPrice={NewPrice}",
                    update.ProductId, update.NewPrice);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "商品ID {ProductId} の価格更新中にエラーが発生しました", update.ProductId);
                errors.Add($"商品ID {update.ProductId}: {ex.Message}");
            }
        }

        // 成功した商品がある場合は通知
        if (succeededCount > 0)
        {
            await _notificationService.NotifyProductChangedAsync(cancellationToken);
        }

        _logger.LogInformation("一括価格更新完了: 成功={SucceededCount}, 失敗={FailedCount}",
            succeededCount, errors.Count);

        return Result.Success(BulkOperationResult.PartiallySucceeded(
            succeededCount,
            errors.Count,
            errors));
    }
}
