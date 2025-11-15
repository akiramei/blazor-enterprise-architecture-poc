using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ApprovePurchaseRequest;

/// <summary>
/// 購買申請承認コマンド
/// </summary>
public class ApprovePurchaseRequestCommand : ICommand<Result<Unit>>
{
    public Guid RequestId { get; init; }
    public string Comment { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
}
