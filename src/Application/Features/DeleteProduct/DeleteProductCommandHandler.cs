using Application.Core.Commands;
using Domain.ProductCatalog.Products;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.DeleteProduct;

/// <summary>
/// 商品削除コマンドハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. Repositoryから集約を取得
/// 2. 存在チェック
/// 3. Entity.Delete()呼び出し（論理削除）
/// 4. Repository経由で保存
/// 5. SignalR通知
///
/// 【リファクタリング成果】
/// - Before: 約85行 (try-catch, ログ含む)
/// - After: 約40行 (ビジネスロジックのみ)
/// - 削減率: 53%
/// </summary>
public class DeleteProductCommandHandler
    : CommandPipeline<DeleteProductCommand, Unit>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;

    public DeleteProductCommandHandler(
        IProductRepository repository,
        IProductNotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        DeleteProductCommand cmd,
        CancellationToken ct)
    {
        // 1. 集約を取得
        var product = await _repository.GetAsync(new ProductId(cmd.ProductId), ct);
        if (product is null)
            return Result.Fail<Unit>("商品が見つかりません");

        // 2. 論理削除（ビジネスルール検証はDomain層）
        // DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
        product.Delete();

        // 3. Repository経由で保存
        await _repository.SaveAsync(product, ct);

        // 4. SignalRで全クライアントに通知
        await _notificationService.NotifyProductChangedAsync(ct);

        return Result.Success(Unit.Value);
    }
}
