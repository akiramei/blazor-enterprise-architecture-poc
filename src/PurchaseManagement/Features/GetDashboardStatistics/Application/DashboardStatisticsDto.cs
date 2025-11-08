namespace GetDashboardStatistics.Application;

/// <summary>
/// ダッシュボード統計情報DTO
///
/// 【パターン: Dashboard Aggregation Query】
///
/// 責務:
/// - 購買管理ダッシュボードに表示する集計データ
/// - リアルタイム集計（Dapper + GROUP BY）
///
/// 使用シナリオ:
/// - 管理者ダッシュボード
/// - 承認者ダッシュボード
/// - KPIモニタリング
/// </summary>
public sealed record DashboardStatisticsDto
{
    /// <summary>
    /// ステータス別件数
    /// </summary>
    public required StatusCountDto StatusCounts { get; init; }

    /// <summary>
    /// 月次統計（過去12ヶ月）
    /// </summary>
    public required List<MonthlyStatisticsDto> MonthlyStatistics { get; init; }

    /// <summary>
    /// トップ高額申請（上位10件）
    /// </summary>
    public required List<TopRequestDto> TopRequests { get; init; }

    /// <summary>
    /// 部門別統計（トップ5部門）
    /// </summary>
    public required List<DepartmentStatisticsDto> DepartmentStatistics { get; init; }

    /// <summary>
    /// 全体サマリー
    /// </summary>
    public required OverallSummaryDto OverallSummary { get; init; }
}

/// <summary>
/// ステータス別件数
/// </summary>
public sealed record StatusCountDto
{
    public int Draft { get; init; }
    public int Submitted { get; init; }
    public int InApproval { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Cancelled { get; init; }

    /// <summary>
    /// 承認待ち件数合計（Submitted + InApproval）
    /// </summary>
    public int PendingTotal => Submitted + InApproval;
}

/// <summary>
/// 月次統計
/// </summary>
public sealed record MonthlyStatisticsDto
{
    /// <summary>
    /// 年月（YYYY-MM）
    /// </summary>
    public required string YearMonth { get; init; }

    /// <summary>
    /// 申請件数
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// 申請総額
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 承認件数
    /// </summary>
    public int ApprovedCount { get; init; }

    /// <summary>
    /// 承認総額
    /// </summary>
    public decimal ApprovedAmount { get; init; }

    /// <summary>
    /// 承認率（%）
    /// </summary>
    public decimal ApprovalRate => RequestCount > 0 ? (decimal)ApprovedCount / RequestCount * 100 : 0;
}

/// <summary>
/// トップ高額申請
/// </summary>
public sealed record TopRequestDto
{
    public Guid Id { get; init; }
    public required string RequestNumber { get; init; }
    public required string Title { get; init; }
    public decimal TotalAmount { get; init; }
    public required string RequesterName { get; init; }
    public DateTime SubmittedAt { get; init; }
    public required string Status { get; init; }
}

/// <summary>
/// 部門別統計
/// </summary>
public sealed record DepartmentStatisticsDto
{
    /// <summary>
    /// 部門名
    /// </summary>
    public required string Department { get; init; }

    /// <summary>
    /// 申請件数
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// 申請総額
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 平均金額
    /// </summary>
    public decimal AverageAmount => RequestCount > 0 ? TotalAmount / RequestCount : 0;
}

/// <summary>
/// 全体サマリー
/// </summary>
public sealed record OverallSummaryDto
{
    /// <summary>
    /// 総申請件数
    /// </summary>
    public int TotalRequests { get; init; }

    /// <summary>
    /// 総申請金額
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 今月の申請件数
    /// </summary>
    public int CurrentMonthRequests { get; init; }

    /// <summary>
    /// 今月の申請金額
    /// </summary>
    public decimal CurrentMonthAmount { get; init; }

    /// <summary>
    /// 平均処理日数
    /// </summary>
    public double AverageProcessingDays { get; init; }

    /// <summary>
    /// 承認待ち最古日数
    /// </summary>
    public int OldestPendingDays { get; init; }
}
