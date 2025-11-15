using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries.Approval;

namespace Application.Features.PurchaseManagement.ApprovePurchaseRequest;

/// <summary>
/// 承認コマンドファクトリー（Intent→Command変換）
/// </summary>
public class ApprovalCommandFactory : IApprovalCommandFactory
{
    public IApprovalCommand CreateCommand(ApprovalIntent intent)
    {
        return new ApprovePurchaseRequestCommand
        {
            PurchaseRequestId = intent.PurchaseRequestId,
            Comments = intent.Comments
        };
    }
}
