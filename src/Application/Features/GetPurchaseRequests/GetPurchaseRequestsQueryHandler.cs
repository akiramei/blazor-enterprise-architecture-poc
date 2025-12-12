using Application.Core.Queries;
using Dapper;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.GetPurchaseRequests;

/// <summary>
/// 購買申請一覧取得クエリハンドラー (工業製品化版)
///
/// 【CQRS Query Handler with Multi-tenant Security】
/// - Dapper使用による効率的なクエリ実行
/// - マルチテナント分離（TenantIdフィルタ必須）
///
/// 【リファクタリング成果】
/// - Before: 約170行 (try-catch, ログ含む)
/// - After: 約130行 (クエリロジックのみ)
/// - 削減率: 24%
/// </summary>
public sealed class GetPurchaseRequestsQueryHandler
    : QueryPipeline<GetPurchaseRequestsQuery, List<PurchaseRequestListItemDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetPurchaseRequestsQueryHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    protected override async Task<Result<List<PurchaseRequestListItemDto>>> ExecuteAsync(
        GetPurchaseRequestsQuery query,
        CancellationToken ct)
    {
        // SECURITY: Get current tenant ID for multi-tenant filtering
        var tenantId = _currentUserService.TenantId;
        if (tenantId == null)
            return Result.Fail<List<PurchaseRequestListItemDto>>("テナント情報が取得できません");

        using var connection = _connectionFactory.CreateConnection();

        var sql = BuildQuery(query);
        var parameters = BuildParameters(query, tenantId.Value);

        var items = await connection.QueryAsync<PurchaseRequestListItemDto>(sql, parameters);

        var result = items
            .Select(item => new PurchaseRequestListItemDto
            {
                Id = item.Id,
                RequestNumber = item.RequestNumber,
                Title = item.Title,
                RequesterName = item.RequesterName,
                Status = item.Status,
                StatusName = GetStatusName(item.Status),
                TotalAmount = item.TotalAmount,
                Currency = item.Currency,
                ApprovalStepCount = item.ApprovalStepCount,
                ApprovedStepCount = item.ApprovedStepCount,
                CreatedAt = item.CreatedAt
            })
            .ToList();

        return Result.Success(result);
    }

    private static string BuildQuery(GetPurchaseRequestsQuery query)
    {
        var orderByClause = query.SortBy switch
        {
            "RequestNumber" => $"pr.\"RequestNumber\" {(query.Ascending ? "ASC" : "DESC")}",
            "Title" => $"pr.\"Title\" {(query.Ascending ? "ASC" : "DESC")}",
            "Status" => $"pr.\"Status\" {(query.Ascending ? "ASC" : "DESC")}",
            "TotalAmount" => $"TotalAmount {(query.Ascending ? "ASC" : "DESC")}",
            "CreatedAt" => $"pr.\"CreatedAt\" {(query.Ascending ? "ASC" : "DESC")}",
            _ => $"pr.\"CreatedAt\" {(query.Ascending ? "ASC" : "DESC")}"
        };

        return $@"
            SELECT
                pr.""Id"",
                pr.""RequestNumber"",
                pr.""RequesterId"",
                pr.""RequesterName"",
                pr.""Title"",
                pr.""Status"",
                pr.""CreatedAt"",
                pr.""SubmittedAt"",
                pr.""ApprovedAt"",
                pr.""RejectedAt"",
                COALESCE(SUM(pri.""Amount""), 0) as TotalAmount,
                COALESCE(MAX(pri.""Currency""), 'JPY') as Currency,
                COUNT(DISTINCT aps.""Id"") as ApprovalStepCount,
                COUNT(DISTINCT CASE WHEN aps.""Status"" = 1 THEN aps.""Id"" END) as ApprovedStepCount
            FROM ""PurchaseRequests"" pr
            LEFT JOIN ""PurchaseRequestItems"" pri ON pr.""Id"" = pri.""PurchaseRequestId""
            LEFT JOIN ""ApprovalSteps"" aps ON pr.""Id"" = aps.""PurchaseRequestId""
            WHERE pr.""TenantId"" = @TenantId
                {(query.Status.HasValue ? "AND pr.\"Status\" = @Status" : "")}
            GROUP BY
                pr.""Id"",
                pr.""RequestNumber"",
                pr.""RequesterId"",
                pr.""RequesterName"",
                pr.""Title"",
                pr.""Status"",
                pr.""CreatedAt"",
                pr.""SubmittedAt"",
                pr.""ApprovedAt"",
                pr.""RejectedAt""
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";
    }

    private static object BuildParameters(GetPurchaseRequestsQuery query, Guid tenantId)
    {
        return new
        {
            TenantId = tenantId, // SECURITY: Multi-tenant filtering
            query.Status,
            query.PageSize,
            Offset = (query.PageNumber - 1) * query.PageSize
        };
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            0 => "下書き",
            1 => "提出済み",
            2 => "1次承認待ち",
            3 => "2次承認待ち",
            4 => "承認済み",
            5 => "却下",
            6 => "キャンセル",
            _ => "不明"
        };
    }
}
