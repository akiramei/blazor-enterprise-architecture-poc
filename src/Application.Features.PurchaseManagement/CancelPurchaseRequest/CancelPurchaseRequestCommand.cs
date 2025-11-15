using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.PurchaseManagement.CancelPurchaseRequest;

/// <summary>
/// 購買申請キャンセルコマンド
/// </summary>
public class CancelPurchaseRequestCommand : ICommand<Result<Unit>>
{
    public Guid RequestId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
