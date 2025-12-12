using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.RejectPurchaseRequest;

/// <summary>
/// 購買申請却下コマンド
/// </summary>
public sealed class RejectPurchaseRequestCommand : ICommand<Result<Unit>>
{
    public Guid RequestId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
}
