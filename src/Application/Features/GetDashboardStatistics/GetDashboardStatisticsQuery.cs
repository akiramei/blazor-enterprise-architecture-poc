using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetDashboardStatistics;

/// <summary>
/// ダッシュボード統計情報取得クエリ
/// </summary>
public class GetDashboardStatisticsQuery : IQuery<Result<DashboardStatisticsDto>>
{
    /// <summary>
    /// 月次統計の取得月数（デフォルト: 12ヶ月）
    /// </summary>
    public int MonthsToInclude { get; init; } = 12;

    /// <summary>
    /// トップ申請の取得件数（デフォルト: 10件）
    /// </summary>
    public int TopRequestsCount { get; init; } = 10;

    /// <summary>
    /// 部門統計の取得件数（デフォルト: 5部門）
    /// </summary>
    public int TopDepartmentsCount { get; init; } = 5;
}

/// <summary>
/// ダッシュボード統計情報DTO
/// </summary>
public class DashboardStatisticsDto
{
    public StatusCountDto StatusCounts { get; init; } = new();
    public List<MonthlyStatisticsDto> MonthlyStatistics { get; init; } = new();
    public List<TopRequestDto> TopRequests { get; init; } = new();
    public List<DepartmentStatisticsDto> DepartmentStatistics { get; init; } = new();
    public OverallSummaryDto OverallSummary { get; init; } = new();
}

public class StatusCountDto
{
    public int Draft { get; init; }
    public int Submitted { get; init; }
    public int InApproval { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Cancelled { get; init; }
    public int PendingTotal => Submitted + InApproval;
}

public class MonthlyStatisticsDto
{
    public string YearMonth { get; init; } = string.Empty;
    public int RequestCount { get; init; }
    public decimal TotalAmount { get; init; }
    public int ApprovedCount { get; init; }
    public decimal ApprovedAmount { get; init; }
    public decimal ApprovalRate => RequestCount > 0 ? (decimal)ApprovedCount / RequestCount * 100 : 0;
}

public class TopRequestDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class DepartmentStatisticsDto
{
    public string Department { get; init; } = string.Empty;
    public int RequestCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AverageAmount => RequestCount > 0 ? TotalAmount / RequestCount : 0;
}

public class OverallSummaryDto
{
    public int TotalRequests { get; init; }
    public decimal TotalAmount { get; init; }
    public int CurrentMonthRequests { get; init; }
    public decimal CurrentMonthAmount { get; init; }
    public double AverageProcessingDays { get; init; }
    public int OldestPendingDays { get; init; }
}
