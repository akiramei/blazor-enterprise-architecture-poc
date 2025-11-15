using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;
using Domain.ProductCatalog.Products;

namespace DeleteProduct.Application;

/// <summary>
/// 商品削除Handler
///
/// 【パターン: 削除系Handler】
///
/// 処理フロー:
/// 1. Repositoryから集約を取得
/// 2. 存在チェック
/// 3. Entity.Delete()呼び出し（ビジネスルール検証）
/// 4. Repository経由で保存
/// 5. 結果を返す
///
/// 実装ガイド:
/// - Handler はオーケストレーションのみ（ビジネスロジックはDomain層）
/// - Entity.Delete()メソッド経由で削除（直接削除しない）
/// - DomainExceptionをキャッチしてResult.Failに変換
/// - 削除後はドメインイベントが自動的に発行される
///
/// AI実装時の注意:
/// - Repository.DeleteAsync() ではなく、Entity.Delete() → Repository.SaveAsync()
/// - ビジネスルール検証はDomain層に委譲（在庫チェックなど）
/// - 論理削除の場合は、物理的にはDBから削除されない
/// - ドメインイベント（ProductDeletedDomainEvent）が発行される
///
/// 論理削除の実装:
/// - Entity.Delete() 内で IsDeleted = true にする
/// - Repository.SaveAsync() で保存（UPDATE文が実行される）
/// - 物理削除の場合は Repository.DeleteAsync() を使用
/// </summary>
public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ILogger<DeleteProductHandler> _logger;

    public DeleteProductHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ILogger<DeleteProductHandler> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        // 1. 集約を取得
        var product = await _repository.GetAsync(new ProductId(command.ProductId), cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("商品が見つかりません: {ProductId}", command.ProductId);
            return Result.Fail("商品が見つかりません");
        }

        // 2. Domainメソッド経由で削除（ビジネスルール検証）
        try
        {
            // ビジネスルール検証:
            // - 在庫がある商品は削除できない
            // - 既に削除済みの商品は削除できない
            product.Delete();
        }
        catch (DomainException ex)
        {
            // ビジネスルール違反
            _logger.LogWarning(ex, "商品削除がドメインルールにより拒否されました: {ProductId}", command.ProductId);
            return Result.Fail(ex.Message);
        }

        // 3. Repository経由で保存（論理削除の場合はUPDATE）
        await _repository.SaveAsync(product, cancellationToken);

        _logger.LogInformation("商品を削除しました: {ProductId}", command.ProductId);

        // 4. SignalRで全クライアントに通知（リアルタイム更新）
        await _notificationService.NotifyProductChangedAsync(cancellationToken);

        return Result.Success();
    }
}
