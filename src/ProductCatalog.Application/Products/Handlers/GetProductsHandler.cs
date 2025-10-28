using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;
using ProductCatalog.Application.Products.Queries;

namespace ProductCatalog.Application.Products.Handlers;

/// <summary>
/// 商品一覧取得ハンドラー
/// </summary>
public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<IEnumerable<ProductDto>>>
{
    private readonly IProductReadRepository _readRepository;
    private readonly ILogger<GetProductsHandler> _logger;

    public GetProductsHandler(
        IProductReadRepository readRepository,
        ILogger<GetProductsHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ProductDto>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("商品一覧を取得しています");

        var products = await _readRepository.GetAllAsync(cancellationToken);

        return Result.Success(products);
    }
}
