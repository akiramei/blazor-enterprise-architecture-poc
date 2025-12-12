using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.UpdateProduct;

/// <summary>
/// 商品更新Command
/// </summary>
public sealed class UpdateProductCommand : ICommand<Result<Unit>>
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public long Version { get; init; }  // 楽観的排他制御用

    /// <summary>
    /// 冪等性キー（重複実行防止）
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
