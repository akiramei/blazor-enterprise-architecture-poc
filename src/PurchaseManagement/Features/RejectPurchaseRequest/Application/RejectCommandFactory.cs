using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

namespace RejectPurchaseRequest.Application;

/// <summary>
/// 却下コマンドファクトリー実装
///
/// 【パターン: Abstract Factory実装】
///
/// ドメイン層のIApprovalCommandFactoryを実装し、
/// Feature層の具体的なコマンドを生成する。
///
/// 設計上の利点:
/// - リフレクション不要（型安全）
/// - コンパイル時に型チェックが可能
/// - AOT（Ahead-of-Time compilation）対応
/// - ドメイン層がFeature層のコマンドに依存しない
/// </summary>
public class RejectCommandFactory : IApprovalCommandFactory
{
    /// <summary>
    /// 承認コマンドを生成
    /// </summary>
    public object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey)
    {
        // NOTE: ApprovePurchaseRequestCommandは別のFeatureにあるため、
        // ここではthrowする。統合ファクトリーで両方を処理する。
        throw new NotImplementedException(
            "ApproveコマンドはApprovePurchaseRequest.Applicationで実装されています。" +
            "CompositeApprovalCommandFactoryを使用してください。"
        );
    }

    /// <summary>
    /// 却下コマンドを生成
    /// </summary>
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
