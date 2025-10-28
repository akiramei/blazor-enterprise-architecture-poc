using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.Commands;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Handlers;

/// <summary>
/// 商品削除ハンドラー
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
        var product = await _repository.GetAsync(new ProductId(command.ProductId), cancellationToken);

        if (product is null)
        {
            return Result.Fail("商品が見つかりません");
        }

        try
        {
            product.Delete(); // ビジネスルール検証
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "商品削除がドメインルールにより拒否されました: {ProductId}", command.ProductId);
            return Result.Fail(ex.Message);
        }

        await _repository.SaveAsync(product, cancellationToken);

        _logger.LogInformation("商品を削除しました: {ProductId}", command.ProductId);

        // SignalRで全クライアントに通知
        await _notificationService.NotifyProductChangedAsync(cancellationToken);

        return Result.Success();
    }
}
