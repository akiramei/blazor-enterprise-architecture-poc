using PurchaseManagement.Shared.Domain.PurchaseRequests;

namespace PurchaseManagement.Shared.Application;

/// <summary>
/// 購買申請リポジトリインターフェース
/// </summary>
public interface IPurchaseRequestRepository
{
    Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken);
}
