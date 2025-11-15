using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Shared.Kernel;
using Domain.ProductCatalog.Products;

namespace BulkDeleteProducts.Application;

/// <summary>
/// 商品一括削除Handler
///
/// 【パターン: 一括処理Handler】
///
/// 処理フロー:
/// 1. ProductIdsをループ
/// 2. 各商品をRepositoryから取得
/// 3. 存在チェック
/// 4. Entity.Delete()呼び出し（ビジネスルール検証）
/// 5. Repository経由で保存
/// 6. 成功/失敗をカウント
/// 7. BulkOperationResultを返す
///
/// 実装ガイド:
/// - 各削除は個別にビジネスルール検証を行う
/// - 1件でも失敗してもループを継続（全件処理）
/// - トランザクションはTransactionBehaviorが管理（全体をロールバック可能）
/// - 失敗した項目のエラーメッセージを記録
/// - 大量データの場合はバッチサイズを制限
///
/// AI実装時の注意:
/// - ループ内で個別にEntity.Delete()を呼ぶ（ビジネスルールを通す）
/// - DomainExceptionをキャッチして続行
/// - エラーメッセージには識別子（ProductId）を含める
/// - パフォーマンスが重要な場合は、事前にRead Modelで検証を検討
/// - ログは適切なレベルで出力
///
/// トランザクション:
/// - TransactionBehaviorが全体を1つのトランザクションで実行
/// - 1件でも失敗したらロールバックするか、部分的に成功を許すかは要件次第
/// - 現在の実装: 全件処理して結果を返す（部分成功を許容）
/// </summary>
public sealed class BulkDeleteProductsHandler
    : IRequestHandler<BulkDeleteProductsCommand, Result<BulkOperationResult>>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ILogger<BulkDeleteProductsHandler> _logger;

    public BulkDeleteProductsHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ILogger<BulkDeleteProductsHandler> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<BulkOperationResult>> Handle(
        BulkDeleteProductsCommand command,
        CancellationToken cancellationToken)
    {
        var productIds = command.ProductIds.ToList();

        // バッチサイズ制限（パフォーマンス考慮）
        if (productIds.Count > 100)
        {
            _logger.LogWarning("一括削除の件数が多すぎます: {Count}件", productIds.Count);
            return Result.Fail<BulkOperationResult>("一括削除は100件までです");
        }

        if (!productIds.Any())
        {
            return Result.Fail<BulkOperationResult>("削除対象の商品が指定されていません");
        }

        var succeededCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        _logger.LogInformation("一括削除を開始します: {Count}件", productIds.Count);

        // 各商品を個別に処理
        foreach (var productId in productIds)
        {
            try
            {
                var product = await _repository.GetAsync(new ProductId(productId), cancellationToken);

                if (product is null)
                {
                    failedCount++;
                    errors.Add($"商品 {productId}: 見つかりません");
                    _logger.LogWarning("商品が見つかりません: {ProductId}", productId);
                    continue;
                }

                try
                {
                    // ビジネスルール検証（在庫チェックなど）
                    product.Delete();

                    await _repository.SaveAsync(product, cancellationToken);

                    succeededCount++;
                    _logger.LogInformation("商品を削除しました: {ProductId}", productId);
                }
                catch (DomainException ex)
                {
                    // ビジネスルール違反
                    failedCount++;
                    errors.Add($"商品 {productId}: {ex.Message}");
                    _logger.LogWarning(ex, "商品削除がドメインルールにより拒否されました: {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                // 予期しないエラー
                failedCount++;
                errors.Add($"商品 {productId}: システムエラーが発生しました");
                _logger.LogError(ex, "商品削除中にエラーが発生しました: {ProductId}", productId);
            }
        }

        _logger.LogInformation(
            "一括削除が完了しました。成功: {Succeeded}件, 失敗: {Failed}件",
            succeededCount,
            failedCount);

        // SignalRで全クライアントに通知（成功した件数が1件以上の場合）
        if (succeededCount > 0)
        {
            await _notificationService.NotifyProductChangedAsync(cancellationToken);
        }

        var result = BulkOperationResult.PartiallySucceeded(succeededCount, failedCount, errors);

        // 結果を返す（成功・失敗の詳細はBulkOperationResultに含まれる）
        // ※ 一部失敗していても、処理自体は成功として扱う
        // ※ 呼び出し側はresult.IsAllSucceededで全成功かどうかを判定できる
        return Result.Success(result);
    }
}
