using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace GetDashboardStatistics.Application;

/// <summary>
/// ダッシュボード統計情報取得ハンドラー
///
/// 【パターン: CQRS Query Handler - Parallel Aggregation】
///
/// 責務:
/// - 5つの集計クエリを並列実行
/// - Dapperによる高速SQL実行
/// - 結果の統合とDTO変換
///
/// パフォーマンス最適化:
/// - Task.WhenAll による並列実行
/// - インデックス活用（Status, SubmittedAt列）
/// - 必要最小限のカラムのみSELECT
/// - GROUP BY / ORDER BY 最適化
///
/// キャッシング:
/// - CachingBehavior適用推奨（5分TTL）
/// - キャッシュキー: "Dashboard:Statistics"
/// </summary>
public sealed class GetDashboardStatisticsHandler
    : IRequestHandler<GetDashboardStatisticsQuery, Result<DashboardStatisticsDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<GetDashboardStatisticsHandler> _logger;

    public GetDashboardStatisticsHandler(
        IDbConnectionFactory connectionFactory,
        ILogger<GetDashboardStatisticsHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Result<DashboardStatisticsDto>> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            // 5つの集計クエリを並列実行
            var (statusCounts, monthlyStats, topRequests, deptStats, overallSummary) = await Task.WhenAll(
                GetStatusCountsAsync(connection),
                GetMonthlyStatisticsAsync(connection, request.MonthsToInclude),
                GetTopRequestsAsync(connection, request.TopRequestsCount),
                GetDepartmentStatisticsAsync(connection, request.TopDepartmentsCount),
                GetOverallSummaryAsync(connection)
            ).ContinueWith(t => (
                t.Result[0] as StatusCountDto ?? new StatusCountDto(),
                t.Result[1] as List<MonthlyStatisticsDto> ?? new List<MonthlyStatisticsDto>(),
                t.Result[2] as List<TopRequestDto> ?? new List<TopRequestDto>(),
                t.Result[3] as List<DepartmentStatisticsDto> ?? new List<DepartmentStatisticsDto>(),
                t.Result[4] as OverallSummaryDto ?? new OverallSummaryDto()
            ), cancellationToken);

            var statistics = new DashboardStatisticsDto
            {
                StatusCounts = statusCounts,
                MonthlyStatistics = monthlyStats,
                TopRequests = topRequests,
                DepartmentStatistics = deptStats,
                OverallSummary = overallSummary
            };

            _logger.LogInformation(
                "ダッシュボード統計を取得しました。[TotalRequests: {TotalRequests}] [PendingCount: {PendingCount}]",
                statistics.OverallSummary.TotalRequests,
                statistics.StatusCounts.PendingTotal);

            return Result.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ダッシュボード統計の取得中にエラーが発生しました");
            return Result.Fail<DashboardStatisticsDto>($"統計情報の取得に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// ステータス別件数取得
    /// </summary>
    private async Task<object> GetStatusCountsAsync(System.Data.IDbConnection connection)
    {
        var sql = @"
            SELECT
                SUM(CASE WHEN ""Status"" = 0 THEN 1 ELSE 0 END) AS Draft,
                SUM(CASE WHEN ""Status"" = 1 THEN 1 ELSE 0 END) AS Submitted,
                SUM(CASE WHEN ""Status"" IN (2, 3, 4) THEN 1 ELSE 0 END) AS InApproval,
                SUM(CASE WHEN ""Status"" = 5 THEN 1 ELSE 0 END) AS Approved,
                SUM(CASE WHEN ""Status"" = 6 THEN 1 ELSE 0 END) AS Rejected,
                SUM(CASE WHEN ""Status"" = 7 THEN 1 ELSE 0 END) AS Cancelled
            FROM pm_PurchaseRequests
        ";

        return await connection.QueryFirstOrDefaultAsync<StatusCountDto>(sql)
               ?? new StatusCountDto();
    }

    /// <summary>
    /// 月次統計取得（過去N ヶ月）
    /// </summary>
    private async Task<object> GetMonthlyStatisticsAsync(
        System.Data.IDbConnection connection,
        int monthsToInclude)
    {
        var sql = @"
            SELECT
                TO_CHAR(""SubmittedAt"", 'YYYY-MM') AS YearMonth,
                COUNT(*) AS RequestCount,
                SUM(""TotalAmount"") AS TotalAmount,
                SUM(CASE WHEN ""Status"" = 5 THEN 1 ELSE 0 END) AS ApprovedCount,
                SUM(CASE WHEN ""Status"" = 5 THEN ""TotalAmount"" ELSE 0 END) AS ApprovedAmount
            FROM pm_PurchaseRequests
            WHERE ""SubmittedAt"" IS NOT NULL
              AND ""SubmittedAt"" >= NOW() - INTERVAL '@Months months'
            GROUP BY TO_CHAR(""SubmittedAt"", 'YYYY-MM')
            ORDER BY YearMonth DESC
        ";

        var results = await connection.QueryAsync<MonthlyStatisticsDto>(
            sql.Replace("@Months", monthsToInclude.ToString()));

        return results.ToList();
    }

    /// <summary>
    /// トップ高額申請取得
    /// </summary>
    private async Task<object> GetTopRequestsAsync(
        System.Data.IDbConnection connection,
        int topCount)
    {
        var sql = @"
            SELECT
                ""Id"",
                ""RequestNumber"",
                ""Title"",
                ""TotalAmount"",
                ""RequesterName"",
                ""SubmittedAt"",
                CASE ""Status""
                    WHEN 0 THEN 'Draft'
                    WHEN 1 THEN 'Submitted'
                    WHEN 2 THEN 'PendingFirstApproval'
                    WHEN 3 THEN 'PendingSecondApproval'
                    WHEN 4 THEN 'PendingFinalApproval'
                    WHEN 5 THEN 'Approved'
                    WHEN 6 THEN 'Rejected'
                    WHEN 7 THEN 'Cancelled'
                END AS Status
            FROM pm_PurchaseRequests
            WHERE ""SubmittedAt"" IS NOT NULL
            ORDER BY ""TotalAmount"" DESC
            LIMIT @TopCount
        ";

        var results = await connection.QueryAsync<TopRequestDto>(sql, new { TopCount = topCount });
        return results.ToList();
    }

    /// <summary>
    /// 部門別統計取得（トップN部門）
    /// 注: 部門情報がない場合は申請者名でグループ化
    /// </summary>
    private async Task<object> GetDepartmentStatisticsAsync(
        System.Data.IDbConnection connection,
        int topCount)
    {
        // NOTE: 現在のスキーマに部門（Department）列がないため、
        // 申請者名でグループ化。将来的に部門列を追加する場合はこのSQLを修正
        var sql = @"
            SELECT
                ""RequesterName"" AS Department,
                COUNT(*) AS RequestCount,
                SUM(""TotalAmount"") AS TotalAmount
            FROM pm_PurchaseRequests
            WHERE ""SubmittedAt"" IS NOT NULL
            GROUP BY ""RequesterName""
            ORDER BY TotalAmount DESC
            LIMIT @TopCount
        ";

        var results = await connection.QueryAsync<DepartmentStatisticsDto>(sql, new { TopCount = topCount });
        return results.ToList();
    }

    /// <summary>
    /// 全体サマリー取得
    /// </summary>
    private async Task<object> GetOverallSummaryAsync(System.Data.IDbConnection connection)
    {
        var sql = @"
            SELECT
                COUNT(*) AS TotalRequests,
                SUM(""TotalAmount"") AS TotalAmount,
                SUM(CASE WHEN DATE_TRUNC('month', ""SubmittedAt"") = DATE_TRUNC('month', NOW())
                    THEN 1 ELSE 0 END) AS CurrentMonthRequests,
                SUM(CASE WHEN DATE_TRUNC('month', ""SubmittedAt"") = DATE_TRUNC('month', NOW())
                    THEN ""TotalAmount"" ELSE 0 END) AS CurrentMonthAmount,
                AVG(CASE WHEN ""ApprovedAt"" IS NOT NULL AND ""SubmittedAt"" IS NOT NULL
                    THEN EXTRACT(EPOCH FROM (""ApprovedAt"" - ""SubmittedAt"")) / 86400
                    ELSE NULL END) AS AverageProcessingDays,
                COALESCE(MAX(CASE WHEN ""Status"" IN (1, 2, 3, 4) AND ""SubmittedAt"" IS NOT NULL
                    THEN EXTRACT(DAY FROM (NOW() - ""SubmittedAt""))
                    ELSE NULL END), 0) AS OldestPendingDays
            FROM pm_PurchaseRequests
            WHERE ""SubmittedAt"" IS NOT NULL
        ";

        return await connection.QueryFirstOrDefaultAsync<OverallSummaryDto>(sql)
               ?? new OverallSummaryDto();
    }
}
