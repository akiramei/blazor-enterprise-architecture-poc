using System.Diagnostics.Metrics;

namespace Shared.Infrastructure.Metrics;

/// <summary>
/// アプリケーションメトリクス定義
///
/// 【パターン: OpenTelemetry Metrics】
///
/// 使用シナリオ:
/// - パフォーマンス監視（レスポンスタイム、スループット）
/// - エラー監視（エラー率、エラー数）
/// - ビジネスメトリクス（作成数、更新数等）
///
/// 実装ガイド:
/// - System.Diagnostics.Metricsを使用
/// - Histogram: 値の分布を記録（実行時間等）
/// - Counter: 単調増加する値を記録（リクエスト数、エラー数等）
/// - MetricsBehaviorから自動的に記録
///
/// OpenTelemetry対応:
/// - Console Exporter（開発環境）
/// - Prometheus Exporter（本番環境、要設定）
/// - Azure Monitor Exporter（Azure環境、要設定）
///
/// メトリクス命名規則（OpenTelemetry推奨）:
/// - スネークケース（snake_case）
/// - ドット区切り（namespace.metric_name）
/// - 単位を含む（_milliseconds, _bytes等）
/// </summary>
public sealed class ApplicationMetrics
{
    private readonly Meter _meter;

    // === パフォーマンスメトリクス ===

    /// <summary>
    /// Command/Query実行時間（ミリ秒）
    /// </summary>
    public Histogram<double> RequestDuration { get; }

    /// <summary>
    /// Command/Query実行回数
    /// </summary>
    public Counter<long> RequestCount { get; }

    /// <summary>
    /// Command/Query実行エラー数
    /// </summary>
    public Counter<long> RequestErrors { get; }

    // === ビジネスメトリクス（将来拡張用） ===

    /// <summary>
    /// 商品作成数
    /// </summary>
    public Counter<long> ProductCreated { get; }

    /// <summary>
    /// 商品更新数
    /// </summary>
    public Counter<long> ProductUpdated { get; }

    /// <summary>
    /// 商品削除数
    /// </summary>
    public Counter<long> ProductDeleted { get; }

    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("ProductCatalog");

        // パフォーマンスメトリクス
        RequestDuration = _meter.CreateHistogram<double>(
            name: "product_catalog.request.duration",
            unit: "ms",
            description: "Command/Query execution duration in milliseconds");

        RequestCount = _meter.CreateCounter<long>(
            name: "product_catalog.request.count",
            unit: "{request}",
            description: "Total number of Command/Query executions");

        RequestErrors = _meter.CreateCounter<long>(
            name: "product_catalog.request.errors",
            unit: "{error}",
            description: "Total number of Command/Query execution errors");

        // ビジネスメトリクス
        ProductCreated = _meter.CreateCounter<long>(
            name: "product_catalog.product.created",
            unit: "{product}",
            description: "Total number of products created");

        ProductUpdated = _meter.CreateCounter<long>(
            name: "product_catalog.product.updated",
            unit: "{product}",
            description: "Total number of products updated");

        ProductDeleted = _meter.CreateCounter<long>(
            name: "product_catalog.product.deleted",
            unit: "{product}",
            description: "Total number of products deleted");
    }
}
