using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Features.Products.CreateProduct;

/// <summary>
/// 商品作成Handler
///
/// 【パターン: 作成系Handler】
///
/// 処理フロー:
/// 1. Domainのファクトリメソッド経由で集約を作成
/// 2. Repository経由で保存
/// 3. 作成されたIDを返す
///
/// 実装ガイド:
/// - ファクトリメソッド（Product.Create()）経由で作成
/// - ビジネスルールはDomain層のファクトリメソッド内で検証
/// - Handler内では取得・保存のオーケストレーションのみ
/// - 作成されたIDを返す（後続処理で使用）
/// - DomainExceptionをキャッチしてResult.Failに変換
///
/// AI実装時の注意:
/// - new Product() ではなく Product.Create() を使う（ファクトリメソッド）
/// - ID生成はDomain層で行う（Handler内でGuid.NewGuid()しない）
/// - 作成日時などの自動設定もDomain層で行う
/// - 例外ハンドリングを適切に
/// - ログは適切なレベルで出力
///
/// 関連パターン:
/// - UpdateProduct: 作成後の編集
/// - GetProductById: 作成後の確認表示
/// </summary>
public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly IProductNotificationService _notificationService;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IProductRepository repository,
        IProductNotificationService notificationService,
        ILogger<CreateProductHandler> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // ファクトリメソッド経由で集約を作成
            // （ビジネスルールの検証はDomain層で行われる）
            var price = new Money(command.Price, command.Currency);
            var product = Product.Create(
                command.Name,
                command.Description,
                price,
                command.InitialStock);

            // Repository経由で保存
            await _repository.SaveAsync(product, cancellationToken);

            _logger.LogInformation(
                "商品を作成しました: {ProductId}, Name: {Name}, Price: {Price}",
                product.Id.Value,
                command.Name,
                command.Price);

            // SignalRで全クライアントに通知（リアルタイム更新）
            await _notificationService.NotifyProductChangedAsync(cancellationToken);

            // 作成されたIDを返す（後続処理で使用可能）
            return Result.Success(product.Id.Value);
        }
        catch (DomainException ex)
        {
            // ビジネスルール違反
            _logger.LogWarning(ex, "商品作成がドメインルールにより拒否されました: Name={Name}", command.Name);
            return Result.Fail<Guid>(ex.Message);
        }
    }
}
