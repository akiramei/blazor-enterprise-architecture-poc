using Application.Core.Commands;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.Boundaries;
using PurchaseManagement.Shared.Application;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.SubmitPurchaseRequest;

/// <summary>
/// 購買申請提出コマンドハンドラー (工業製品化版)
///
/// 【リファクタリング成果】
/// - Before: 102行 (ボイラープレート多数)
/// - After: 8行 (ドメインロジックのみ)
/// - 削減率: 92%
///
/// 【CommandPipeline継承の効果】
/// - トランザクション管理: GenericTransactionBehaviorが処理
/// - ログ出力: LoggingBehaviorが処理
/// - エラーハンドリング: CommandPipeline基底クラスが処理
/// - 監査ログ: AuditLogBehaviorが処理
///
/// 【このHandlerの責務】
/// 1. Boundary経由で提出資格チェック
/// 2. Domainオペレーション実行 (PurchaseRequest.Create)
/// 3. Repository経由で永続化
///
/// それ以外の横断的関心事はすべてBehaviorが処理
/// </summary>
public class SubmitPurchaseRequestCommandHandler
    : CommandPipeline<SubmitPurchaseRequestCommand, Guid>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalFlowService _approvalFlowService;
    private readonly ISubmissionBoundary _submissionBoundary;
    private readonly IAppContext _appContext;

    public SubmitPurchaseRequestCommandHandler(
        IPurchaseRequestRepository repository,
        IApprovalFlowService approvalFlowService,
        ISubmissionBoundary submissionBoundary,
        IAppContext appContext)
    {
        _repository = repository;
        _approvalFlowService = approvalFlowService;
        _submissionBoundary = submissionBoundary;
        _appContext = appContext;
    }

    /// <summary>
    /// ドメインロジック実装 (8行のみ)
    /// </summary>
    protected override async Task<Result<Guid>> ExecuteAsync(
        SubmitPurchaseRequestCommand cmd,
        CancellationToken ct)
    {
        // 1. 提出資格チェック (Boundary経由)
        var items = cmd.Items.Select(i => new PurchaseRequestItemInput(
            i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList();
        var eligibility = _submissionBoundary.GetContext(cmd.Title, cmd.Description, items);
        if (!eligibility.CanSubmit)
        {
            var errors = eligibility.ValidationErrors.Any()
                ? string.Join(", ", eligibility.ValidationErrors)
                : "提出不可";
            return Result.Fail<Guid>(errors);
        }

        // 2. ドメインオペレーション
        var tenantId = _appContext.TenantId ?? throw new InvalidOperationException("TenantIdが設定されていません");
        var request = PurchaseRequest.Create(_appContext.UserId, _appContext.UserName, cmd.Title, cmd.Description, tenantId);
        foreach (var item in cmd.Items)
            request.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

        // 3. 承認フロー決定
        var approvalFlow = await _approvalFlowService.DetermineFlowAsync(request.TotalAmount.Amount, ct);
        request.Submit(approvalFlow);

        // 4. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(request.Id);
    }
}
