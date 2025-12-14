using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.CreateProduct;

/// <summary>
/// 商品作成Command
/// </summary>
public sealed class CreateProductCommand : ICommand<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "JPY";
    public int InitialStock { get; init; }

    /// <summary>
    /// 冪等性キー（重複実行防止）
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
