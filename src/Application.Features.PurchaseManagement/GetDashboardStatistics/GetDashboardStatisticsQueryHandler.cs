using Application.Core.Queries;
using Dapper;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace Application.Features.PurchaseManagement.GetDashboardStatistics;

/// <summary>
/// ダッシュボード統計情報取得クエリハンドラー (工業製品化版)
///
/// 【CQRS Query Handler - Parallel Aggregation with Multi-tenant Security】
///
/// 【リファクタリング成果】
/// - Before: 約305行 (try-catch, ログ, 5つのプライベートメソッド含む)
/// - After: 約290行 (クエリロジックのみ)
/// - 削減率: 5% (複雑な集計クエリのため削減幅は小さい)
///
/// 【パフォーマンス最適化】
/// - Task.WhenAll による並列実行
/// - 各クエリで独立したDBコネクション使用（コネクション競合回避）
/// - インデックス活用（TenantId, Status, SubmittedAt）
/// </summary>
public class GetDashboardStatisticsQueryHandler
    : QueryPipeline<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardStatisticsQueryHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    protected override async Task<Result<DashboardStatisticsDto>> ExecuteAsync(
        GetDashboardStatisticsQuery query,
        CancellationToken ct)
    {
        // SECURITY: Get current tenant ID for multi-tenant filtering
        var tenantId = _currentUserService.TenantId;
        if (tenantId == null)
            return Result.Fail<DashboardStatisticsDto>("テナント情報が取得できません");

        // IMPORTANT: 各クエリで独立したコネクションを使用
        // Npgsql/ADO.NET のコネクションはスレッドセーフではないため、
        // 1本のコネクションを Task.WhenAll で使い回すと
        // "The connection was already in use by another operation" エラーが発生
        // 接続プールがあるため、パフォーマンスへの影響は最小限

        // 5つの集計クエリを並列実行（各クエリが独立したコネクションを使用）
        var (statusCounts, monthlyStats, topRequests, deptStats, overallSummary) = await Task.WhenAll(
            GetStatusCountsAsync(tenantId.Value),
            GetMonthlyStatisticsAsync(tenantId.Value, query.MonthsToInclude),
            GetTopRequestsAsync(tenantId.Value, query.TopRequestsCount),
            GetDepartmentStatisticsAsync(tenantId.Value, query.TopDepartmentsCount),
            GetOverallSummaryAsync(tenantId.Value)
        ).ContinueWith(t => (
            t.Result[0] as StatusCountDto ?? new StatusCountDto(),
            t.Result[1] as List<MonthlyStatisticsDto> ?? new List<MonthlyStatisticsDto>(),
            t.Result[2] as List<TopRequestDto> ?? new List<TopRequestDto>(),
            t.Result[3] as List<DepartmentStatisticsDto> ?? new List<DepartmentStatisticsDto>(),
            t.Result[4] as OverallSummaryDto ?? new OverallSummaryDto()
        ), ct);

        var statistics = new DashboardStatisticsDto
        {
            StatusCounts = statusCounts,
            MonthlyStatistics = monthlyStats,
            TopRequests = topRequests,
            DepartmentStatistics = deptStats,
            OverallSummary = overallSummary
        };

        return Result.Success(statistics);
    }

    /// <summary>
    /// ステータス別件数取得
    /// </summary>
    private async Task<object> GetStatusCountsAsync(Guid tenantId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT
                SUM(CASE WHEN ""Status"" = 0 THEN 1 ELSE 0 END) AS Draft,
                SUM(CASE WHEN ""Status"" = 1 THEN 1 ELSE 0 END) AS Submitted,
                SUM(CASE WHEN ""Status"" IN (2, 3, 4) THEN 1 ELSE 0 END) AS InApproval,
                SUM(CASE WHEN ""Status"" = 5 THEN 1 ELSE 0 END) AS Approved,
                SUM(CASE WHEN ""Status"" = 6 THEN 1 ELSE 0 END) AS Rejected,
                SUM(CASE WHEN ""Status"" = 7 THEN 1 ELSE 0 END) AS Cancelled
            FROM ""PurchaseRequests""
            WHERE ""TenantId"" = @TenantId
        ";

        return await connection.QueryFirstOrDefaultAsync<StatusCountDto>(sql, new { TenantId = tenantId })
               ?? new StatusCountDto();
    }

    /// <summary>
    /// 月次統計取得（過去N ヶ月）
    /// </summary>
    private async Task<object> GetMonthlyStatisticsAsync(
        Guid tenantId,
        int monthsToInclude)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            WITH RequestTotals AS (
                SELECT
                    pr.""Id"",
                    pr.""SubmittedAt"",
                    pr.""Status"",
                    COALESCE((
                        SELECT SUM(pri.""Amount"")
                        FROM ""PurchaseRequestItems"" pri
                        WHERE pri.""PurchaseRequestId"" = pr.""Id""
                    ), 0) AS TotalAmount
                FROM ""PurchaseRequests"" pr
                WHERE pr.""TenantId"" = @TenantId
                  AND pr.""SubmittedAt"" IS NOT NULL
                  AND pr.""SubmittedAt"" >= NOW() - INTERVAL '@Months months'
            )
            SELECT
                TO_CHAR(""SubmittedAt"", 'YYYY-MM') AS YearMonth,
                COUNT(*) AS RequestCount,
                SUM(TotalAmount) AS TotalAmount,
                SUM(CASE WHEN ""Status"" = 5 THEN 1 ELSE 0 END) AS ApprovedCount,
                SUM(CASE WHEN ""Status"" = 5 THEN TotalAmount ELSE 0 END) AS ApprovedAmount
            FROM RequestTotals
            GROUP BY TO_CHAR(""SubmittedAt"", 'YYYY-MM')
            ORDER BY YearMonth DESC
        ";

        var results = await connection.QueryAsync<MonthlyStatisticsDto>(
            sql.Replace("@Months", monthsToInclude.ToString()),
            new { TenantId = tenantId });

        return results.ToList();
    }

    /// <summary>
    /// トップ高額申請取得
    /// </summary>
    private async Task<object> GetTopRequestsAsync(
        Guid tenantId,
        int topCount)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT
                pr.""Id"",
                pr.""RequestNumber"",
                pr.""Title"",
                COALESCE((
                    SELECT SUM(pri.""Amount"")
                    FROM ""PurchaseRequestItems"" pri
                    WHERE pri.""PurchaseRequestId"" = pr.""Id""
                ), 0) AS TotalAmount,
                pr.""RequesterName"",
                pr.""SubmittedAt"",
                CASE pr.""Status""
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'PendingFirstApproval'
                    WHEN 3 THEN 'PendingSecondApproval'
                    WHEN 4 THEN 'PendingFinalApproval'
                    WHEN 5 THEN 'Approved'
                    WHEN 6 THEN 'Rejected'
                    WHEN 7 THEN 'Cancelled'
                END AS Status
            FROM ""PurchaseRequests"" pr
            WHERE pr.""TenantId"" = @TenantId
              AND pr.""SubmittedAt"" IS NOT NULL
            ORDER BY COALESCE((
                SELECT SUM(pri.""Amount"")
                FROM ""PurchaseRequestItems"" pri
                WHERE pri.""PurchaseRequestId"" = pr.""Id""
            ), 0) DESC
            LIMIT @TopCount
        ";

        var results = await connection.QueryAsync<TopRequestDto>(sql, new { TenantId = tenantId, TopCount = topCount });
        return results.ToList();
    }

    /// <summary>
    /// 部門別統計取得（トップN部門）
    /// 注: 部門情報がない場合は申請者名でグループ化
    /// </summary>
    private async Task<object> GetDepartmentStatisticsAsync(
        Guid tenantId,
        int topCount)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            WITH RequestTotals AS (
                SELECT
                    pr.""RequesterName"",
                    COALESCE((
                        SELECT SUM(pri.""Amount"")
                        FROM ""PurchaseRequestItems"" pri
                        WHERE pri.""PurchaseRequestId"" = pr.""Id""
                    ), 0) AS TotalAmount
                FROM ""PurchaseRequests"" pr
                WHERE pr.""TenantId"" = @TenantId
                  AND pr.""SubmittedAt"" IS NOT NULL
            )
            SELECT
                ""RequesterName"" AS Department,
                COUNT(*) AS RequestCount,
                SUM(TotalAmount) AS TotalAmount
            FROM RequestTotals
            GROUP BY ""RequesterName""
            ORDER BY SUM(TotalAmount) DESC
            LIMIT @TopCount
        ";

        var results = await connection.QueryAsync<DepartmentStatisticsDto>(sql, new { TenantId = tenantId, TopCount = topCount });
        return results.ToList();
    }

    /// <summary>
    /// 全体サマリー取得
    /// </summary>
    private async Task<object> GetOverallSummaryAsync(Guid tenantId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            WITH RequestTotals AS (
                SELECT
                    pr.""Id"",
                    pr.""SubmittedAt"",
                    pr.""ApprovedAt"",
                    pr.""Status"",
                    COALESCE((
                        SELECT SUM(pri.""Amount"")
                        FROM ""PurchaseRequestItems"" pri
                        WHERE pri.""PurchaseRequestId"" = pr.""Id""
                    ), 0) AS TotalAmount
                FROM ""PurchaseRequests"" pr
                WHERE pr.""TenantId"" = @TenantId
                  AND pr.""SubmittedAt"" IS NOT NULL
            )
            SELECT
                COUNT(*) AS TotalRequests,
                SUM(TotalAmount) AS TotalAmount,
                SUM(CASE WHEN DATE_TRUNC('month', ""SubmittedAt"") = DATE_TRUNC('month', NOW())
                    THEN 1 ELSE 0 END) AS CurrentMonthRequests,
                SUM(CASE WHEN DATE_TRUNC('month', ""SubmittedAt"") = DATE_TRUNC('month', NOW())
                    THEN TotalAmount ELSE 0 END) AS CurrentMonthAmount,
                AVG(CASE WHEN ""ApprovedAt"" IS NOT NULL AND ""SubmittedAt"" IS NOT NULL
                    THEN EXTRACT(EPOCH FROM (""ApprovedAt"" - ""SubmittedAt"")) / 86400
                    ELSE NULL END) AS AverageProcessingDays,
                COALESCE(MAX(CASE WHEN ""Status"" IN (1, 2, 3, 4) AND ""SubmittedAt"" IS NOT NULL
                    THEN EXTRACT(DAY FROM (NOW() - ""SubmittedAt""))
                    ELSE NULL END), 0) AS OldestPendingDays
            FROM RequestTotals
        ";

        return await connection.QueryFirstOrDefaultAsync<OverallSummaryDto>(sql, new { TenantId = tenantId })
               ?? new OverallSummaryDto();
    }
}
