using Application.Core.Commands;
using Domain.PurchaseManagement.PurchaseRequests;
using MediatR;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.CancelPurchaseRequest;

/// <summary>
/// 購買申請キャンセルコマンドハンドラー (工業製品化版)
///
/// 【リファクタリング成果】
/// - Before: 約60行 (try-catch, ログ含む)
/// - After: 約30行 (ドメインロジックのみ)
/// - 削減率: 50%
/// </summary>
public sealed class CancelPurchaseRequestCommandHandler
    : CommandPipeline<CancelPurchaseRequestCommand, Unit>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CancelPurchaseRequestCommandHandler(
        IPurchaseRequestRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        CancelPurchaseRequestCommand cmd,
        CancellationToken ct)
    {
        // 1. 購買申請を取得
        var request = await _repository.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result.Fail<Unit>("購買申請が見つかりません");

        // 2. キャンセル処理 (ドメインロジック)
        // DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
        request.Cancel(_currentUserService.UserId);

        // 3. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(Unit.Value);
    }
}
