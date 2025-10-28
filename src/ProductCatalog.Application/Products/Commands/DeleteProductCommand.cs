using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;

namespace ProductCatalog.Application.Products.Commands;

/// <summary>
/// 商品削除Command
/// </summary>
public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result>;
