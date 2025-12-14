using Application.Core.Queries;
using Domain.PurchaseManagement.PurchaseRequests;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.GetPurchaseRequestById;

/// <summary>
/// 購買申請詳細取得クエリハンドラー (工業製品化版)
///
/// 【パターン: Entity直接返却（Boundary活用のため）】
/// UI層でバウンダリーを活用するため、DTOではなくエンティティを返す
/// バウンダリーがUIメタデータを提供するため、QueryHandlerでのDTO変換は不要
/// </summary>
public sealed class GetPurchaseRequestByIdQueryHandler
    : QueryPipeline<GetPurchaseRequestByIdQuery, PurchaseRequest>
{
    private readonly IPurchaseRequestRepository _repository;

    public GetPurchaseRequestByIdQueryHandler(IPurchaseRequestRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<PurchaseRequest>> ExecuteAsync(
        GetPurchaseRequestByIdQuery query,
        CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(query.Id, ct);
        if (request is null)
            return Result.Fail<PurchaseRequest>("購買申請が見つかりません");

        return Result.Success(request);
    }
}
