using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Application.Core.Commands;
using Shared.Kernel;
using Domain.ProductCatalog.Products;

namespace Application.Features.BulkUpdateProductPrices;

/// <summary>
/// 商品価格一括更新ハンドラ
///
/// 【パターン: バルク更新 - Handler + CommandPipeline】
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
public sealed class BulkUpdateProductPricesCommandHandler
    : CommandPipeline<BulkUpdateProductPricesCommand, BulkOperationResult>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private const int MaxUpdateCount = 100;

    public BulkUpdateProductPricesCommandHandler(
        IProductRepository repository,
        IProductNotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    protected override async Task<Result<BulkOperationResult>> ExecuteAsync(
        BulkUpdateProductPricesCommand command,
        CancellationToken ct)
    {
        // バリデーション
        if (!command.Updates.Any())
        {
            return Result.Fail<BulkOperationResult>("更新対象の商品が指定されていません");
        }

        if (command.Updates.Count > MaxUpdateCount)
        {
            return Result.Fail<BulkOperationResult>($"一括更新は{MaxUpdateCount}件までです");
        }

        var succeededCount = 0;
        var errors = new List<string>();

        // 各商品を更新
        foreach (var update in command.Updates)
        {
            try
            {
                var product = await _repository.GetAsync(new ProductId(update.ProductId), ct);

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
                await _repository.SaveAsync(product, ct);
                succeededCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"商品ID {update.ProductId}: {ex.Message}");
            }
        }

        // 成功した商品がある場合は通知
        if (succeededCount > 0)
        {
            await _notificationService.NotifyProductChangedAsync(ct);
        }

        return Result.Success(BulkOperationResult.PartiallySucceeded(
            succeededCount,
            errors.Count,
            errors));
    }
}
