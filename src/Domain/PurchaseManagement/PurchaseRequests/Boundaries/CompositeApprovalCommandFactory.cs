namespace Domain.PurchaseManagement.PurchaseRequests.Boundaries;

/// <summary>
/// 複合承認コマンドファクトリー：承認と却下の両方のコマンドを生成
///
/// 【パターン: Composite + Abstract Factory】
///
/// 各Feature層のファクトリーを組み合わせて、完全なコマンド生成機能を提供する。
/// VSAのFeature独立性を保ちながら、ドメイン層に統一インターフェースを提供。
///
/// 設計上の利点:
/// - Feature層の独立性を維持
/// - 各Featureが自分のコマンドだけを知っていればよい
/// - ドメイン層は統一されたファクトリーを使用
/// - 新しいコマンドの追加が容易
/// </summary>
public class CompositeApprovalCommandFactory : IApprovalCommandFactory
{
    private readonly IApprovalCommandFactory _approveFactory;
    private readonly IApprovalCommandFactory _rejectFactory;

    public CompositeApprovalCommandFactory(
        IApprovalCommandFactory approveFactory,
        IApprovalCommandFactory rejectFactory)
    {
        _approveFactory = approveFactory;
        _rejectFactory = rejectFactory;
    }

    /// <summary>
    /// 承認コマンドを生成（ApprovePurchaseRequest.Applicationへ委譲）
    /// </summary>
    public object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey)
    {
        return _approveFactory.CreateApproveCommand(requestId, comment, idempotencyKey);
    }

    /// <summary>
    /// 却下コマンドを生成（RejectPurchaseRequest.Applicationへ委譲）
    /// </summary>
    public object CreateRejectCommand(Guid requestId, string reason, string idempotencyKey)
    {
        return _rejectFactory.CreateRejectCommand(requestId, reason, idempotencyKey);
    }
}
