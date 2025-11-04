using System.Diagnostics;
using MediatR;
using Shared.Infrastructure.Metrics;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// メトリクス収集Behavior
///
/// 【パターン: Pipeline Behavior - Metrics】
///
/// 使用シナリオ:
/// - すべてのCommand/Queryの実行時間を自動計測
/// - 成功/失敗数の自動カウント
/// - パフォーマンス監視・ボトルネック特定
///
/// 実装方針:
/// - OpenTelemetry Metricsを使用
/// - すべてのリクエストを自動計測（オーバーヘッド最小）
/// - タグによる分類（request_type, success/failure）
///
/// Pipeline順序:
/// - すべてのBehaviorの最外層（最初）で実行して全体の実行時間を計測
/// - LoggingBehaviorより前に実行されるため、ログ出力時間も含めて計測
/// </summary>
/// <typeparam name="TRequest">リクエスト型</typeparam>
/// <typeparam name="TResponse">レスポンス型</typeparam>
public sealed class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ApplicationMetrics _metrics;

    public MetricsBehavior(ApplicationMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            // リクエスト実行
            var response = await next();
            success = true;
            return response;
        }
        finally
        {
            stopwatch.Stop();

            // メトリクス記録
            var duration = stopwatch.Elapsed.TotalMilliseconds;
            var status = success ? "success" : "failure";

            // 実行時間を記録（タグ付き）
            _metrics.RequestDuration.Record(
                duration,
                new KeyValuePair<string, object?>("request_type", requestType),
                new KeyValuePair<string, object?>("status", status));

            // リクエスト数をカウント
            _metrics.RequestCount.Add(
                1,
                new KeyValuePair<string, object?>("request_type", requestType),
                new KeyValuePair<string, object?>("status", status));

            // エラー数をカウント（失敗時のみ）
            if (!success)
            {
                _metrics.RequestErrors.Add(
                    1,
                    new KeyValuePair<string, object?>("request_type", requestType));
            }
        }
    }
}
