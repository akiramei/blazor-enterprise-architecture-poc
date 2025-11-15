using Application.Core.Commands;
using Domain.ProductCatalog.Products;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;

namespace Application.Features.UpdateProduct;

/// <summary>
/// 商品更新コマンドハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. Repositoryから集約を取得
/// 2. 存在チェック
/// 3. 楽観的排他制御（Versionチェック）
/// 4. Domainメソッド経由で変更
/// 5. Repository経由で保存
/// 6. キャッシュ無効化＆SignalR通知
///
/// 【リファクタリング成果】
/// - Before: 約115行 (try-catch, ログ含む)
/// - After: 約70行 (ビジネスロジックのみ)
/// - 削減率: 39%
/// </summary>
public class UpdateProductCommandHandler
    : CommandPipeline<UpdateProductCommand, Unit>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public UpdateProductCommandHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ICacheInvalidationService cacheInvalidation)
    {
        _repository = repository;
        _notificationService = notificationService;
        _cacheInvalidation = cacheInvalidation;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        UpdateProductCommand cmd,
        CancellationToken ct)
    {
        // 1. 集約を取得
        var product = await _repository.GetAsync(new ProductId(cmd.ProductId), ct);
        if (product is null)
            return Result.Fail<Unit>("商品が見つかりません");

        // 2. 楽観的排他制御（Versionチェック）
        if (product.Version != cmd.Version)
        {
            return Result.Fail<Unit>(
                "他のユーザーによって更新されています。" +
                "最新のデータを取得してから再度お試しください。");
        }

        // 3. Domainメソッド経由で変更
        // DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
        product.ChangeName(cmd.Name);
        product.ChangeDescription(cmd.Description);
        product.ChangePrice(new Money(cmd.Price));
        product.ChangeStock(cmd.Stock);

        // 4. Repository経由で保存
        await _repository.SaveAsync(product, ct);

        // 5. キャッシュ無効化（GetProductByIdQueryのキャッシュをクリア）
        _cacheInvalidation.InvalidateProduct(cmd.ProductId);

        // 6. SignalRで全クライアントに通知（リアルタイム更新）
        await _notificationService.NotifyProductChangedAsync(ct);

        return Result.Success(Unit.Value);
    }
}
