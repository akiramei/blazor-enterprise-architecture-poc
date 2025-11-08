using Dapper;
using MediatR;
using PurchaseManagement.Shared.Application;
using Shared.Application;
using Shared.Application.Interfaces;

namespace GetPurchaseRequestById.Application;

/// <summary>
/// 購買申請詳細取得ハンドラ
///
/// 【パターン: CQRS Query Handler】
///
/// 責務:
/// - Dapperを使用してPostgreSQLから購買申請詳細を取得
/// - 承認ステップと品目を含む完全なデータ取得
/// - DTOへの変換
///
/// AI実装時の注意:
/// - Multiple result setsを使用して1回のクエリで全データ取得
/// - ステータス名の変換はアプリケーション層で実施
/// </summary>
public sealed class GetPurchaseRequestByIdHandler : IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDetailDto?>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetPurchaseRequestByIdHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<PurchaseRequestDetailDto?>> Handle(
        GetPurchaseRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                -- Purchase Request
                SELECT
                    ""Id"",
                    ""RequestNumber"",
                    ""RequesterId"",
                    ""RequesterName"",
                    ""Title"",
                    ""Description"",
                    ""Status"",
                    ""CreatedAt"",
                    ""SubmittedAt"",
                    ""ApprovedAt"",
                    ""RejectedAt"",
                    ""CancelledAt""
                FROM ""PurchaseRequests""
                WHERE ""Id"" = @Id;

                -- Approval Steps
                SELECT
                    ""Id"",
                    ""StepNumber"",
                    ""ApproverId"",
                    ""ApproverName"",
                    ""ApproverRole"",
                    ""Status"",
                    ""Comment"",
                    ""ApprovedAt"",
                    ""RejectedAt""
                FROM ""ApprovalSteps""
                WHERE ""PurchaseRequestId"" = @Id
                ORDER BY ""StepNumber"";

                -- Items
                SELECT
                    ""Id"",
                    ""ProductId"",
                    ""ProductName"",
                    ""UnitPrice"",
                    ""Currency"",
                    ""Quantity"",
                    ""Amount""
                FROM ""PurchaseRequestItems""
                WHERE ""PurchaseRequestId"" = @Id;";

            using var multi = await connection.QueryMultipleAsync(sql, new { query.Id });

            var purchaseRequest = await multi.ReadSingleOrDefaultAsync<PurchaseRequestDbModel>();
            if (purchaseRequest == null)
            {
                return Result.Success<PurchaseRequestDetailDto?>(null);
            }

            var approvalSteps = (await multi.ReadAsync<ApprovalStepDbModel>()).ToList();
            var items = (await multi.ReadAsync<PurchaseRequestItemDetailDto>()).ToList();

            var totalAmount = items.Sum(i => i.Amount);
            var currency = items.FirstOrDefault()?.Currency ?? "JPY";

            var result = new PurchaseRequestDetailDto
            {
                Id = purchaseRequest.Id,
                RequestNumber = purchaseRequest.RequestNumber,
                RequesterId = purchaseRequest.RequesterId,
                RequesterName = purchaseRequest.RequesterName,
                Title = purchaseRequest.Title,
                Description = purchaseRequest.Description,
                Status = purchaseRequest.Status,
                StatusName = GetStatusName(purchaseRequest.Status),
                CreatedAt = purchaseRequest.CreatedAt,
                SubmittedAt = purchaseRequest.SubmittedAt,
                ApprovedAt = purchaseRequest.ApprovedAt,
                RejectedAt = purchaseRequest.RejectedAt,
                CancelledAt = purchaseRequest.CancelledAt,
                ApprovalSteps = approvalSteps.Select(s => new ApprovalStepDto
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    ApproverId = s.ApproverId,
                    ApproverName = s.ApproverName,
                    ApproverRole = s.ApproverRole,
                    Status = s.Status,
                    StatusName = GetApprovalStepStatusName(s.Status),
                    Comment = s.Comment,
                    ApprovedAt = s.ApprovedAt,
                    RejectedAt = s.RejectedAt
                }).ToList(),
                Items = items,
                TotalAmount = totalAmount,
                Currency = currency
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

    private sealed record PurchaseRequestDbModel
    {
        public required Guid Id { get; init; }
        public required string RequestNumber { get; init; }
        public required Guid RequesterId { get; init; }
        public required string RequesterName { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required int Status { get; init; }
        public required DateTime CreatedAt { get; init; }
        public DateTime? SubmittedAt { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public DateTime? RejectedAt { get; init; }
        public DateTime? CancelledAt { get; init; }
    }

    private sealed record ApprovalStepDbModel
    {
        public required Guid Id { get; init; }
        public required int StepNumber { get; init; }
        public required Guid ApproverId { get; init; }
        public required string ApproverName { get; init; }
        public required string ApproverRole { get; init; }
        public required int Status { get; init; }
        public string? Comment { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public DateTime? RejectedAt { get; init; }
    }
}
