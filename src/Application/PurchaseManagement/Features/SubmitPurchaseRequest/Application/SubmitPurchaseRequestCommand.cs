using Shared.Application;
using Shared.Application.Interfaces;

namespace SubmitPurchaseRequest.Application;

/// <summary>
/// 購買申請提出コマンド
/// </summary>
public sealed record SubmitPurchaseRequestCommand : ICommand<Result<Guid>>
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<PurchaseRequestItemDto> Items { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record PurchaseRequestItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);
