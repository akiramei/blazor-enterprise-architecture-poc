using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// テスト用ApprovalFlowService
///
/// 固定のApproverIdを使用して、テストでの承認者チェックをパスできるようにする
/// </summary>
public sealed class TestApprovalFlowService : IApprovalFlowService
{
    // テスト用の固定承認者ID（admin@example.comのUserIdと同じにする）
    // 実際のUserIdはIdentityDataSeederで作成される
    private static readonly Guid TestApproverId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private const decimal FirstThreshold = 100_000m;  // 10万円
    private const decimal SecondThreshold = 500_000m; // 50万円

    public Task<ApprovalFlow> DetermineFlowAsync(decimal totalAmount, CancellationToken cancellationToken)
    {
        ApprovalFlow flow;

        if (totalAmount < FirstThreshold)
        {
            // 10万円未満: 1段階承認
            flow = ApprovalFlow.SingleStep(
                TestApproverId,
                "テスト承認者1",
                "直属上司"
            );
        }
        else if (totalAmount < SecondThreshold)
        {
            // 10万円以上50万円未満: 2段階承認
            flow = ApprovalFlow.TwoStep(
                TestApproverId,
                "テスト承認者1",
                TestApproverId, // テストでは同じユーザーが2段階目も承認
                "部門長"
            );
        }
        else
        {
            // 50万円以上: 3段階承認
            flow = ApprovalFlow.ThreeStep(
                TestApproverId,
                "テスト承認者1",
                TestApproverId, // テストでは同じユーザーが2段階目も承認
                "部門長",
                TestApproverId, // テストでは同じユーザーが3段階目も承認
                "経営層"
            );
        }

        return Task.FromResult(flow);
    }
}
