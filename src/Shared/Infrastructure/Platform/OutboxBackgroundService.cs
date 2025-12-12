using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;

namespace Shared.Infrastructure.Platform;

/// <summary>
/// Outbox パターンの背景サービス
/// 定期的に各BCのOutboxから未処理メッセージを取得し、外部システムに配信する
///
/// 【トランザクショナルOutboxパターン - ディスパッチ側】
///
/// 責務:
/// - IEnumerable<IOutboxReader> を使って全BCのOutboxを巡回
/// - 各BCのメッセージを外部システムに配信（Message Broker、HTTP、SignalR等）
/// - 配信成功/失敗の状態管理
///
/// 設計:
/// - BC独立性: 各BCのOutboxReaderを通じて読み取り（直接DBアクセスしない）
/// - 拡張性: 新しいBCを追加する場合はIOutboxReaderを実装してDI登録するだけ
/// - リトライ: 失敗時は自動リトライ（最大3回）
/// </summary>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private readonly int _batchSize = 20;

    public OutboxBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Background Service started");

        // サービス起動時に登録されているOutboxReaderの数を確認
        using (var scope = _serviceProvider.CreateScope())
        {
            var readers = scope.ServiceProvider.GetServices<IOutboxReader>().ToList();
            _logger.LogInformation("Found {Count} Outbox Reader(s) registered", readers.Count);

            if (readers.Count == 0)
            {
                _logger.LogWarning("No IOutboxReader implementations registered. Outbox processing will be skipped.");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Background Service stopped");
    }

    private async Task ProcessAllOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        // 全てのBCのOutboxReaderを取得
        var readers = scope.ServiceProvider.GetServices<IOutboxReader>();

        foreach (var reader in readers)
        {
            try
            {
                await ProcessOutboxReaderAsync(reader, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox reader: {ReaderType}", reader.GetType().Name);
            }
        }
    }

    private async Task ProcessOutboxReaderAsync(IOutboxReader reader, CancellationToken cancellationToken)
    {
        var readerName = reader.GetType().Name;

        // 未処理メッセージを取得
        var messages = await reader.DequeueAsync(_batchSize, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "[{ReaderName}] Processing {Count} outbox message(s)",
            readerName,
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // TODO: 実際の配信ロジック（Message Broker、HTTP、SignalR等）
                // 現在は処理済みとしてマークするのみ
                _logger.LogInformation(
                    "[{ReaderName}] Processing message: {MessageId}, Type: {Type}",
                    readerName,
                    message.Id,
                    message.Type);

                // メッセージを配信（実装例: RabbitMQ, Azure Service Bus, AWS SNS/SQS等）
                // await PublishToMessageBrokerAsync(message, cancellationToken);

                // 配信成功
                await reader.MarkSucceededAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "[{ReaderName}] Message processed successfully: {MessageId}",
                    readerName,
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[{ReaderName}] Failed to process message: {MessageId}",
                    readerName,
                    message.Id);

                // 配信失敗（リトライ管理はOutboxReaderのMarkFailedAsyncで行われる）
                await reader.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    // TODO: メッセージブローカーへの配信実装例
    // private async Task PublishToMessageBrokerAsync(OutboxMessage message, CancellationToken cancellationToken)
    // {
    //     // RabbitMQ, Azure Service Bus, AWS SNS/SQS等への配信ロジック
    //     // var channel = _messageBrokerConnection.CreateModel();
    //     // var body = Encoding.UTF8.GetBytes(message.Content);
    //     // channel.BasicPublish(
    //     //     exchange: "domain-events",
    //     //     routingKey: message.Type,
    //     //     basicProperties: null,
    //     //     body: body);
    // }
}
