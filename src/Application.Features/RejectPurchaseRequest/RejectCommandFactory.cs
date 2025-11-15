using Domain.PurchaseManagement.PurchaseRequests.Boundaries;

namespace Application.Features.RejectPurchaseRequest;

/// <summary>
/// 却下コマンドファクトリー（Intent→Command変換）
/// </summary>
public class RejectCommandFactory : IApprovalCommandFactory
{
    public object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey)
    {
        // This factory only handles reject commands
        throw new NotSupportedException("Use ApprovalCommandFactory for approve commands");
    }

    public object CreateRejectCommand(Guid requestId, string reason, string idempotencyKey)
    {
        return new RejectPurchaseRequestCommand
        {
            RequestId = requestId,
            Reason = reason,
            IdempotencyKey = idempotencyKey
        };
    }
}
