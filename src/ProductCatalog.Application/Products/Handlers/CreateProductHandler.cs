using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Products.Commands;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Products.Handlers;

/// <summary>
/// 商品作成ハンドラー
/// </summary>
public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IProductRepository repository,
        ILogger<CreateProductHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var price = new Money(command.Price, command.Currency);
            var product = Product.Create(
                command.Name,
                command.Description,
                price,
                command.InitialStock);

            await _repository.SaveAsync(product, cancellationToken);

            _logger.LogInformation("商品を作成しました: {ProductId}", product.Id.Value);

            return Result.Success(product.Id.Value);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "商品作成が失敗しました");
            return Result.Fail<Guid>(ex.Message);
        }
    }
}
