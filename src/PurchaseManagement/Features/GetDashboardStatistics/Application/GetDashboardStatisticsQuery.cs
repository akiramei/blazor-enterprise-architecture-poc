using MediatR;
using Shared.Application;

namespace GetDashboardStatistics.Application;

/// <summary>
/// ダッシュボード統計情報取得クエリ
///
/// 【パターン: CQRS Query - Aggregation】
///
/// 責務:
/// - 複数の集計クエリを並行実行
/// - Dapperによる高速集計
/// - キャッシュ可能（CachingBehavior適用）
///
/// パフォーマンス:
/// - 5つの集計クエリを並列実行
/// - インデックス活用（Status, SubmittedAt, TotalAmount）
/// - 結果キャッシュ（5分TTL推奨）
///
/// 使用シナリオ:
/// - ダッシュボードページロード
/// - 定期リフレッシュ（30秒〜1分）
/// - KPIモニタリング
/// </summary>
public sealed record GetDashboardStatisticsQuery : IRequest<Result<DashboardStatisticsDto>>
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
