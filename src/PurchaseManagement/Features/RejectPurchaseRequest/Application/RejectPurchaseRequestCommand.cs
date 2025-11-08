using Shared.Application;
using Shared.Application.Interfaces;

namespace RejectPurchaseRequest.Application;

/// <summary>
/// 購買申請却下コマンド
/// </summary>
public sealed record RejectPurchaseRequestCommand : ICommand<Result<Unit>>
{
    public required Guid RequestId { get; init; }
    public required string Reason { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record Unit
{
    public static Unit Value { get; } = new();
    private Unit() { }
}
