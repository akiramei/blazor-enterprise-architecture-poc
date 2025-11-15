using MediatR;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace GetPurchaseRequestById.Application;

/// <summary>
/// 購買申請詳細取得ハンドラ
///
/// 【パターン: CQRS Query Handler with EF Core Repository】
///
/// 責務:
/// - EF Core Repository経由で購買申請詳細を取得
/// - Global Query Filterによるマルチテナント分離を確保（SECURITY）
/// - 承認ステップと品目を含む完全なデータ取得
/// - DTOへの変換
///
/// セキュリティ:
/// - **CRITICAL**: EF Core Repositoryを使用することで、Global Query Filterが自動適用される
/// - IDbConnectionを直接使用すると、Global Query Filterをバイパスしてしまい、
///   他テナントのデータが閲覧可能になる（セキュリティ脆弱性）
///
/// AI実装時の注意:
/// - 読み取り専用クエリでも、マルチテナントデータの場合は必ずEF Core Repositoryを使用
/// - ステータス名の変換はアプリケーション層で実施
/// - OwnsMany()で設定されている子エンティティは自動的にIncludeされる
/// </summary>
public sealed class GetPurchaseRequestByIdHandler : IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDetailDto?>>
{
    private readonly IPurchaseRequestRepository _repository;

    public GetPurchaseRequestByIdHandler(IPurchaseRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PurchaseRequestDetailDto?>> Handle(
        GetPurchaseRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Global Query Filterが自動適用されるため、他テナントのデータは取得できない
            var purchaseRequest = await _repository.GetByIdAsync(query.Id, cancellationToken);

            if (purchaseRequest == null)
            {
                return Result.Success<PurchaseRequestDetailDto?>(null);
            }

            // ドメインエンティティからDTOへ変換
            var result = new PurchaseRequestDetailDto
            {
                Id = purchaseRequest.Id,
                RequestNumber = purchaseRequest.RequestNumber.Value,
                RequesterId = purchaseRequest.RequesterId,
                RequesterName = purchaseRequest.RequesterName,
                Title = purchaseRequest.Title,
                Description = purchaseRequest.Description,
                Status = (int)purchaseRequest.Status,
                StatusName = GetStatusName((int)purchaseRequest.Status),
                CreatedAt = purchaseRequest.CreatedAt,
                SubmittedAt = purchaseRequest.SubmittedAt,
                ApprovedAt = purchaseRequest.ApprovedAt,
                RejectedAt = purchaseRequest.RejectedAt,
                CancelledAt = purchaseRequest.CancelledAt,
                ApprovalSteps = purchaseRequest.ApprovalSteps.Select(s => new ApprovalStepDto
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    ApproverId = s.ApproverId,
                    ApproverName = s.ApproverName,
                    ApproverRole = s.ApproverRole,
                    Status = (int)s.Status,
                    StatusName = GetApprovalStepStatusName((int)s.Status),
                    Comment = s.Comment,
                    ApprovedAt = s.ApprovedAt,
                    RejectedAt = s.RejectedAt
                }).ToList(),
                Items = purchaseRequest.Items.Select(i => new PurchaseRequestItemDetailDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice.Amount,
                    Currency = i.UnitPrice.Currency,
                    Quantity = i.Quantity,
                    Amount = i.Amount.Amount
                }).ToList(),
                TotalAmount = purchaseRequest.TotalAmount.Amount,
                Currency = purchaseRequest.Items.FirstOrDefault()?.UnitPrice.Currency ?? "JPY"
            };

            return Result.Success<PurchaseRequestDetailDto?>(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<PurchaseRequestDetailDto?>(
                $"購買申請詳細の取得中にエラーが発生しました: {ex.Message}");
        }
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            0 => "下書き",
            1 => "提出済み",
            2 => "1次承認待ち",
            3 => "2次承認待ち",
            4 => "3次承認待ち",
            5 => "承認済み",
            6 => "却下",
            7 => "キャンセル",
            _ => "不明"
        };
    }

    private static string GetApprovalStepStatusName(int status)
    {
        return status switch
        {
            0 => "保留中",
            1 => "承認済み",
            2 => "却下",
            _ => "不明"
        };
    }
}
