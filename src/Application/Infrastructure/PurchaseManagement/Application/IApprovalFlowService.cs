using Domain.PurchaseManagement.PurchaseRequests;

namespace PurchaseManagement.Shared.Application;

/// <summary>
/// 承認フロー決定サービスインターフェース
/// </summary>
public interface IApprovalFlowService
{
    Task<ApprovalFlow> DetermineFlowAsync(decimal totalAmount, CancellationToken cancellationToken);
}
