using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Features.Products.UpdateProduct;

/// <summary>
/// 商品更新Handler
///
/// 【パターン: 更新系Handler】
///
/// 処理フロー:
/// 1. Repositoryから集約を取得
/// 2. 存在チェック
/// 3. 楽観的排他制御（Versionチェック）
/// 4. Domainメソッド経由で変更
/// 5. Repository経由で保存
/// 6. 結果を返す
///
/// 実装ガイド:
/// - Handler はオーケストレーションのみ（ビジネスロジックはDomain層）
/// - DomainExceptionをキャッチしてResult.Failに変換
/// - 楽観的排他制御の競合は明確なエラーメッセージで返す
/// - ログは適切なレベルで出力（情報、警告、エラー）
///
/// AI実装時の注意:
/// - 必ずDomainメソッド経由で変更する（product.ChangeXxx()）
/// - 直接フィールドを変更しない（product.Name = xxx は NG）
/// - Versionチェックを忘れずに実装
/// - 変更の順序は重要（依存関係がある場合）
/// </summary>
public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ILogger<UpdateProductHandler> _logger;

    public UpdateProductHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ILogger<UpdateProductHandler> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        // 1. 集約を取得
        var product = await _repository.GetAsync(new ProductId(command.ProductId), cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("商品が見つかりません: {ProductId}", command.ProductId);
            return Result.Fail("商品が見つかりません");
        }

        // 2. 楽観的排他制御（Versionチェック）
        if (product.Version != command.Version)
        {
            _logger.LogWarning(
                "楽観的排他制御: バージョン不一致。商品: {ProductId}, 期待: {Expected}, 実際: {Actual}",
                command.ProductId,
                command.Version,
                product.Version);

            return Result.Fail(
                "他のユーザーによって更新されています。" +
                "最新のデータを取得してから再度お試しください。");
        }

        // 3. Domainメソッド経由で変更
        try
        {
            // 各変更メソッドは内部でビジネスルールを検証する
            product.ChangeName(command.Name);
            product.ChangeDescription(command.Description);
            product.ChangePrice(new Money(command.Price));
            product.ChangeStock(command.Stock);
        }
        catch (DomainException ex)
        {
            // ビジネスルール違反
            _logger.LogWarning(ex, "商品更新がドメインルールにより拒否されました: {ProductId}", command.ProductId);
            return Result.Fail(ex.Message);
        }

        // 4. Repository経由で保存
        await _repository.SaveAsync(product, cancellationToken);

        _logger.LogInformation("商品を更新しました: {ProductId}, Name: {Name}", command.ProductId, command.Name);

        // 5. SignalRで全クライアントに通知（リアルタイム更新）
        await _notificationService.NotifyProductChangedAsync(cancellationToken);

        return Result.Success();
    }
}
