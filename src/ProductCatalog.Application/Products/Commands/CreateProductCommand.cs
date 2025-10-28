using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Application.Products.Commands;

/// <summary>
/// 商品作成Command
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock) : ICommand<Result<Guid>>;
