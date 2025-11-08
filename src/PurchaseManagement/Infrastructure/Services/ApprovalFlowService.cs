using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;

namespace PurchaseManagement.Infrastructure.Services;

/// <summary>
/// 承認フロー決定サービス
///
/// 【パターン: Domain Service Implementation】
///
/// 責務:
/// - 購買申請の金額に応じて適切な承認フローを決定
///
/// ビジネスルール:
/// - 10万円未満: 1段階承認（直属上司のみ）
/// - 10万円以上50万円未満: 2段階承認（直属上司 → 部門長）
/// - 50万円以上: 3段階承認（直属上司 → 部門長 → 経営層）
///
/// 実装方針:
/// - 現在はハードコーディング（将来的にはDBから承認者を取得）
/// - 承認者はGuid.NewGuid()で仮のIDを生成
/// - 実運用では外部サービス（HRシステム、ADなど）から承認者情報を取得
///
/// AI実装時の注意:
/// - 将来的には承認者マスタを参照する設計に変更予定
/// - 現時点ではシンプルな実装で十分
/// </summary>
public sealed class ApprovalFlowService : IApprovalFlowService
{
    // ビジネスルール: 承認段階の金額しきい値
    private const decimal FirstThreshold = 100_000m;  // 10万円
    private const decimal SecondThreshold = 500_000m; // 50万円

    public Task<ApprovalFlow> DetermineFlowAsync(decimal totalAmount, CancellationToken cancellationToken)
    {
        ApprovalFlow flow;

        if (totalAmount < FirstThreshold)
        {
            // 10万円未満: 1段階承認
            flow = ApprovalFlow.SingleStep(
                Guid.NewGuid(), // TODO: 実際の直属上司IDを取得
                "直属上司（仮）",
                "直属上司"
            );
        }
        else if (totalAmount < SecondThreshold)
        {
            // 10万円以上50万円未満: 2段階承認
            flow = ApprovalFlow.TwoStep(
                Guid.NewGuid(), // TODO: 実際の直属上司IDを取得
                "直属上司（仮）",
                Guid.NewGuid(), // TODO: 実際の部門長IDを取得
                "部門長（仮）"
            );
        }
        else
        {
            // 50万円以上: 3段階承認
            flow = ApprovalFlow.ThreeStep(
                Guid.NewGuid(), // TODO: 実際の直属上司IDを取得
                "直属上司（仮）",
                Guid.NewGuid(), // TODO: 実際の部門長IDを取得
                "部門長（仮）",
                Guid.NewGuid(), // TODO: 実際の経営層IDを取得
                "経営層（仮）"
            );
        }

        return Task.FromResult(flow);
    }
}
