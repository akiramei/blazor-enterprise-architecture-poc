using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries.Approval;

namespace Application.Features.PurchaseManagement.RejectPurchaseRequest;

/// <summary>
/// 却下コマンドファクトリー（Intent→Command変換）
/// </summary>
public class RejectCommandFactory : IApprovalCommandFactory
{
    public IApprovalCommand CreateCommand(ApprovalIntent intent)
    {
        return new RejectPurchaseRequestCommand
        {
            PurchaseRequestId = intent.PurchaseRequestId,
            Comments = intent.Comments,
            Reason = intent.Reason
        };
    }
}
