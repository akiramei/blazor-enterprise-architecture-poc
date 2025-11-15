using Application.Core.Queries;
using Dapper;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.PurchaseManagement.GetPendingApprovals;

/// <summary>
/// 承認待ち申請一覧取得クエリハンドラー (工業製品化版)
///
/// 【CQRS Query Handler with Multi-tenant Security】
///
/// 【リファクタリング成果】
/// - Before: 約155行 (try-catch, ログ含む)
/// - After: 約85行 (クエリロジックのみ)
/// - 削減率: 45%
///
/// 【セキュリティ】
/// - マルチテナント分離（TenantIdフィルタ必須）
/// - 承認者IDによるフィルタ（現在のユーザーのみ）
/// </summary>
public class GetPendingApprovalsQueryHandler
    : QueryPipeline<GetPendingApprovalsQuery, List<PendingApprovalDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingApprovalsQueryHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    protected override async Task<Result<List<PendingApprovalDto>>> ExecuteAsync(
        GetPendingApprovalsQuery query,
        CancellationToken ct)
    {
        // SECURITY: Get current user ID and tenant ID
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (userId == Guid.Empty)
            return Result.Fail<List<PendingApprovalDto>>("ユーザー情報が取得できません");

        if (tenantId == null)
            return Result.Fail<List<PendingApprovalDto>>("テナント情報が取得できません");

        using var connection = _connectionFactory.CreateConnection();

        // ソート順を決定
        var orderByClause = query.SortBy switch
        {
            "TotalAmount" => query.Ascending ? "TotalAmount ASC" : "TotalAmount DESC",
            "SubmittedAt" => query.Ascending ? "pr.\"SubmittedAt\" ASC" : "pr.\"SubmittedAt\" DESC",
            "RequestNumber" => query.Ascending ? "pr.\"RequestNumber\" ASC" : "pr.\"RequestNumber\" DESC",
            _ => "TotalAmount DESC" // デフォルト: 金額降順
        };

        // SQL: 承認待ち申請を取得
        // SECURITY FIX: 全承認待ちステータスを含む (1, 2, 3, 4)
        //   1 = Submitted
        //   2 = PendingFirstApproval
        //   3 = PendingSecondApproval
        //   4 = PendingFinalApproval
        var sql = $@"
            SELECT
                pr.""Id"" AS PurchaseRequestId,
                pr.""RequestNumber"",
                pr.""Title"",
                COALESCE((
                    SELECT SUM(pri.""Amount"")
                    FROM ""PurchaseRequestItems"" pri
                    WHERE pri.""PurchaseRequestId"" = pr.""Id""
                ), 0) AS TotalAmount,
                pr.""RequesterId"",
                pr.""RequesterName"",
                pr.""SubmittedAt"",
                ast.""StepNumber"" AS CurrentStepNumber,
                (SELECT COUNT(*) FROM ""ApprovalSteps"" WHERE ""PurchaseRequestId"" = pr.""Id"") AS TotalSteps
            FROM ""PurchaseRequests"" pr
            INNER JOIN ""ApprovalSteps"" ast
                ON pr.""Id"" = ast.""PurchaseRequestId""
            WHERE pr.""TenantId"" = @TenantId
              AND ast.""ApproverId"" = @UserId
              AND ast.""Status"" = 0  -- ApprovalStepStatus.Pending = 0
              AND pr.""Status"" IN (1, 2, 3, 4)  -- 全承認待ちステータスを含む
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset
        ";

        var offset = (query.PageNumber - 1) * query.PageSize;

        var results = await connection.QueryAsync<PendingApprovalDto>(
            sql,
            new
            {
                TenantId = tenantId.Value, // SECURITY: Multi-tenant filtering
                UserId = userId,
                PageSize = query.PageSize,
                Offset = offset
            });

        return Result.Success(results.ToList());
    }
}
