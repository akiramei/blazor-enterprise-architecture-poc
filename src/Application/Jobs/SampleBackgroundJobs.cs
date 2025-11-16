using Hangfire;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

/// <summary>
/// サンプル バックグラウンドジョブ
///
/// 【パターン: Background Job Examples】
///
/// Hangfireで実行可能な3種類のジョブパターンを示します:
/// 1. **Fire-and-Forget**: 即座に1回だけ実行（例: メール送信）
/// 2. **Delayed Job**: 指定時間後に実行（例: リマインダー通知）
/// 3. **Recurring Job**: 定期実行（例: 日次レポート生成）
///
/// 使用例:
/// ```csharp
/// // Fire-and-Forget
/// BackgroundJob.Enqueue<SampleBackgroundJobs>(x => x.SendWelcomeEmail("user@example.com"));
///
/// // Delayed Job (1時間後)
/// BackgroundJob.Schedule<SampleBackgroundJobs>(x => x.SendReminder("user@example.com"), TimeSpan.FromHours(1));
///
/// // Recurring Job (毎日午前2時)
/// RecurringJob.AddOrUpdate<SampleBackgroundJobs>("daily-cleanup", x => x.CleanupOldData(), Cron.Daily(2));
/// ```
///
/// AI実装時の注意:
/// - ジョブメソッドはpublicでなければならない
/// - 引数は全てシリアライズ可能な型を使用
/// - 長時間実行するジョブはCancellationTokenを受け取るべき
/// - 例外はHangfireが自動でリトライする（デフォルト10回）
/// </summary>
public sealed class SampleBackgroundJobs
{
    private readonly ILogger<SampleBackgroundJobs> _logger;

    public SampleBackgroundJobs(ILogger<SampleBackgroundJobs> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fire-and-Forget ジョブの例: ウェルカムメール送信
    /// </summary>
    [Queue("default")]
    public void SendWelcomeEmail(string emailAddress)
    {
        _logger.LogInformation("ウェルカムメールを送信中: {EmailAddress}", emailAddress);

        // TODO: 実際のメール送信ロジック
        // await _emailService.SendAsync(emailAddress, "Welcome!", "Thank you for signing up!");

        _logger.LogInformation("ウェルカムメールを送信しました: {EmailAddress}", emailAddress);
    }

    /// <summary>
    /// Delayed ジョブの例: リマインダー通知
    /// </summary>
    [Queue("default")]
    public void SendReminder(string emailAddress)
    {
        _logger.LogInformation("リマインダー通知を送信中: {EmailAddress}", emailAddress);

        // TODO: 実際のリマインダー送信ロジック

        _logger.LogInformation("リマインダー通知を送信しました: {EmailAddress}", emailAddress);
    }

    /// <summary>
    /// Recurring ジョブの例: 古いデータのクリーンアップ
    /// </summary>
    [Queue("default")]
    public async Task CleanupOldData(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("古いデータのクリーンアップを開始します");

        try
        {
            // TODO: 実際のクリーンアップロジック
            // - 古いログの削除
            // - 期限切れトークンの削除
            // - 一時ファイルの削除

            await Task.Delay(100, cancellationToken); // Placeholder

            _logger.LogInformation("古いデータのクリーンアップが完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "古いデータのクリーンアップ中にエラーが発生しました");
            throw; // Hangfireが自動でリトライ
        }
    }

    /// <summary>
    /// 高優先度ジョブの例: 緊急通知
    /// </summary>
    [Queue("critical")]
    public void SendUrgentNotification(string message)
    {
        _logger.LogWarning("緊急通知を送信中: {Message}", message);

        // TODO: 実際の緊急通知ロジック

        _logger.LogWarning("緊急通知を送信しました: {Message}", message);
    }

    /// <summary>
    /// 低優先度ジョブの例: レポート生成
    /// </summary>
    [Queue("low")]
    public async Task GenerateMonthlyReport(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("月次レポート生成を開始します");

        try
        {
            // TODO: 実際のレポート生成ロジック
            // - データ集計
            // - PDF/Excel生成
            // - ファイル保存

            await Task.Delay(1000, cancellationToken); // Placeholder for long-running task

            _logger.LogInformation("月次レポート生成が完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "月次レポート生成中にエラーが発生しました");
            throw;
        }
    }
}
