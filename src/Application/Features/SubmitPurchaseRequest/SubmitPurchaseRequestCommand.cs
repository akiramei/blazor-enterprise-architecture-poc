using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.SubmitPurchaseRequest;

/// <summary>
/// 購買申請提出コマンド
/// </summary>
public class SubmitPurchaseRequestCommand : ICommand<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<PurchaseRequestItemDto> Items { get; init; } = new();
}

/// <summary>
/// 購買申請明細DTO
/// </summary>
public record PurchaseRequestItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
