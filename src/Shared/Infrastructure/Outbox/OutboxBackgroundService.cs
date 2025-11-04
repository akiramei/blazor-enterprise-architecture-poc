using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;

namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Outbox メッセージを定期的に処理するバックグラウンドサービス
///
/// 【トランザクショナルOutboxパターン - ディスパッチャー】
///
/// 責務:
/// - 全BCのOutboxを巡回してメッセージを取得
/// - メッセージのディスパッチ（SignalR, MessageQueue等）
/// - 処理結果の記録（成功/失敗）
///
/// 設計:
/// - IEnumerable<IOutboxReader>で複数BCをサポート
/// - 各BCは独立してスケールアウト可能
/// - ラウンドロビン的に巡回（フェアスケジューリング）
///
/// 実装パターン:
/// - ProductCatalogOutboxReader
/// - OrderOutboxReader（将来）
/// - InventoryOutboxReader（将来）
/// </summary>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 100;

    public OutboxBackgroundService(
        IServiceProvider _serviceProvider,
        ILogger<OutboxBackgroundService> logger)
    {
        this._serviceProvider = _serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundService が開始されました。");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllOutboxesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox メッセージの処理中にエラーが発生しました。");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundService が停止されました。");
    }

    private async Task ProcessAllOutboxesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var readers = scope.ServiceProvider.GetServices<IOutboxReader>();

        if (!readers.Any())
        {
            _logger.LogWarning("IOutboxReader が登録されていません。");
            return;
        }

        // 各BCのOutboxを巡回
        foreach (var reader in readers)
        {
            try
            {
                await ProcessOutboxAsync(reader, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Outbox Reader [{ReaderType}] の処理中にエラーが発生しました。",
                    reader.GetType().Name);
            }
        }
    }

    private async Task ProcessOutboxAsync(IOutboxReader reader, CancellationToken cancellationToken)
    {
        // 未処理のメッセージを取得
        var messages = await reader.DequeueAsync(BatchSize, cancellationToken);

        if (!messages.Any())
        {
            return;
        }

        _logger.LogInformation(
            "[{ReaderType}] Outbox メッセージを {Count} 件処理します。",
            reader.GetType().Name,
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // メッセージをディスパッチ
                await DispatchMessageAsync(message, cancellationToken);

                // 成功としてマーク
                await reader.MarkSucceededAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Outbox メッセージを処理しました。 [Id: {MessageId}] [Type: {MessageType}]",
                    message.Id,
                    message.Type);
            }
            catch (Exception ex)
            {
                // 失敗としてマーク
                await reader.MarkFailedAsync(message.Id, ex.Message, cancellationToken);

                _logger.LogError(ex,
                    "Outbox メッセージの処理に失敗しました。 [Id: {MessageId}] [Type: {MessageType}]",
                    message.Id,
                    message.Type);
            }
        }
    }

    private async Task DispatchMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        // 実際のメッセージディスパッチロジック
        // 現在は単純にログ出力のみ（実運用では SignalR や Message Queue への配信を実装）
        _logger.LogDebug(
            "メッセージディスパッチ中: {Type} - {Content}",
            message.Type,
            message.Content);

        // TODO: SignalR Hub への配信
        // TODO: Message Queue（RabbitMQ, Azure Service Bus等）への送信
        // TODO: Webhook呼び出し

        await Task.CompletedTask;
    }
}
