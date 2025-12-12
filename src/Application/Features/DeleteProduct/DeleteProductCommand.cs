using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.DeleteProduct;

/// <summary>
/// 商品削除Command
/// </summary>
public sealed class DeleteProductCommand : ICommand<Result<Unit>>
{
    public Guid ProductId { get; init; }

    /// <summary>
    /// 冪等性キー（重複実行防止）
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}
