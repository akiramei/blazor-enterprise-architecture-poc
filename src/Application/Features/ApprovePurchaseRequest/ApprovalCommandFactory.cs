using Domain.PurchaseManagement.Boundaries;

namespace Application.Features.ApprovePurchaseRequest;

/// <summary>
/// 承認コマンドファクトリー（Intent→Command変換）
/// </summary>
public sealed class ApprovalCommandFactory : IApprovalCommandFactory
{
    public object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey)
    {
        return new ApprovePurchaseRequestCommand
        {
            RequestId = requestId,
            Comment = comment,
            IdempotencyKey = idempotencyKey
        };
    }

    public object CreateRejectCommand(Guid requestId, string reason, string idempotencyKey)
    {
        // This factory only handles approve commands
        throw new NotSupportedException("Use RejectCommandFactory for reject commands");
    }
}
