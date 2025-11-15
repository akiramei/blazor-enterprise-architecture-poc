using Application.Core.Commands;
using Domain.ProductCatalog.Products;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;

namespace Application.Features.ProductCatalog.CreateProduct;

/// <summary>
/// 商品作成コマンドハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. Domainのファクトリメソッド経由で集約を作成
/// 2. Repository経由で保存
/// 3. SignalRで全クライアントに通知（リアルタイム更新）
/// 4. 作成されたIDを返す
///
/// 【リファクタリング成果】
/// - Before: 約89行 (try-catch, ログ含む)
/// - After: 約40行 (ビジネスロジックのみ)
/// - 削減率: 55%
///
/// 【実装ガイド】
/// - ファクトリメソッド（Product.Create()）経由で作成
/// - ビジネスルールはDomain層のファクトリメソッド内で検証
/// - Handler内では取得・保存のオーケストレーションのみ
/// - DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
/// </summary>
public class CreateProductCommandHandler
    : CommandPipeline<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;

    public CreateProductCommandHandler(
        IProductRepository repository,
        IProductNotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        CreateProductCommand cmd,
        CancellationToken ct)
    {
        // 1. ファクトリメソッド経由で集約を作成
        // （ビジネスルールの検証はDomain層で行われる）
        // DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
        var price = new Money(cmd.Price, cmd.Currency);
        var product = Product.Create(
            cmd.Name,
            cmd.Description,
            price,
            cmd.InitialStock);

        // 2. Repository経由で保存
        await _repository.SaveAsync(product, ct);

        // 3. SignalRで全クライアントに通知（リアルタイム更新）
        await _notificationService.NotifyProductChangedAsync(ct);

        // 4. 作成されたIDを返す（後続処理で使用可能）
        return Result.Success(product.Id.Value);
    }
}
