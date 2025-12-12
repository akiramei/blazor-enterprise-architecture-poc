using Application.Features.GetPurchaseRequestById;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PurchaseManagement.Infrastructure.Persistence;
using Domain.PurchaseManagement.PurchaseRequests;
using Shared.Application;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// EF Core版GetPurchaseRequestByIdハンドラ（Fast テスト用）
///
/// Dapperハンドラと同等の結果を返すが、EF Coreを使用してInMemory/SQLiteデータベースに対応
/// </summary>
public sealed class EfGetPurchaseRequestByIdHandler
    : IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequest>>
{
    private readonly PurchaseManagementDbContext _context;

    public EfGetPurchaseRequestByIdHandler(PurchaseManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PurchaseRequest>> Handle(
        GetPurchaseRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // OwnsMany で設定された子エンティティ (ApprovalSteps, Items) は
            // 自動的にロードされるため、Include は不要
            // Attachments は独立エンティティだが、必要に応じて別途取得可能
            var purchaseRequest = await _context.PurchaseRequests
                .FirstOrDefaultAsync(pr => pr.Id == query.Id, cancellationToken);

            if (purchaseRequest is null)
            {
                return Result.Fail<PurchaseRequest>("購買申請が見つかりません");
            }

            return Result.Success(purchaseRequest);
        }
        catch (Exception ex)
        {
            return Result.Fail<PurchaseRequest>(
                $"購買申請詳細の取得中にエラーが発生しました: {ex.Message}");
        }
    }
}
