using Shared.Application;
using Shared.Application.Interfaces;

namespace ApprovePurchaseRequest.Application;

/// <summary>
/// 購買申請承認コマンド
/// </summary>
public sealed record ApprovePurchaseRequestCommand : ICommand<Result<Unit>>
{
    public required Guid RequestId { get; init; }
    public required string Comment { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record Unit
{
    public static Unit Value { get; } = new();
    private Unit() { }
}
