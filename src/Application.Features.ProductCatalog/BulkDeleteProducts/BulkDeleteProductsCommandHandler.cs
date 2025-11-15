using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Application.Core.Commands;
using Shared.Kernel;
using Domain.ProductCatalog.Products;

namespace Application.Features.ProductCatalog.BulkDeleteProducts;

/// <summary>
/// 商品一括削除Handler
///
/// 【パターン: 一括処理Handler + CommandPipeline】
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
public sealed class BulkDeleteProductsCommandHandler
    : CommandPipeline<BulkDeleteProductsCommand, BulkOperationResult>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;

    public BulkDeleteProductsCommandHandler(
        IProductRepository repository,
        IProductNotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    protected override async Task<Result<BulkOperationResult>> ExecuteAsync(
        BulkDeleteProductsCommand command,
        CancellationToken ct)
    {
        var productIds = command.ProductIds.ToList();

        // バッチサイズ制限（パフォーマンス考慮）
        if (productIds.Count > 100)
        {
            return Result.Fail<BulkOperationResult>("一括削除は100件までです");
        }

        if (!productIds.Any())
        {
            return Result.Fail<BulkOperationResult>("削除対象の商品が指定されていません");
        }

        var succeededCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        // 各商品を個別に処理
        foreach (var productId in productIds)
        {
            try
            {
                var product = await _repository.GetAsync(new ProductId(productId), ct);

                if (product is null)
                {
                    failedCount++;
                    errors.Add($"商品 {productId}: 見つかりません");
                    continue;
                }

                try
                {
                    // ビジネスルール検証（在庫チェックなど）
                    product.Delete();

                    await _repository.SaveAsync(product, ct);

                    succeededCount++;
                }
                catch (DomainException ex)
                {
                    // ビジネスルール違反
                    failedCount++;
                    errors.Add($"商品 {productId}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // 予期しないエラー
                failedCount++;
                errors.Add($"商品 {productId}: システムエラーが発生しました");
            }
        }

        // SignalRで全クライアントに通知（成功した件数が1件以上の場合）
        if (succeededCount > 0)
        {
            await _notificationService.NotifyProductChangedAsync(ct);
        }

        var result = BulkOperationResult.PartiallySucceeded(succeededCount, failedCount, errors);

        // 結果を返す（成功・失敗の詳細はBulkOperationResultに含まれる）
        // ※ 一部失敗していても、処理自体は成功として扱う
        // ※ 呼び出し側はresult.IsAllSucceededで全成功かどうかを判定できる
        return Result.Success(result);
    }
}
