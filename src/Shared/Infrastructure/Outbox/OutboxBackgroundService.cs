using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Domain.Outbox;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Outbox メッセージを定期的に処理するバックグラウンドサービス
/// </summary>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public OutboxBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundService が開始されました。");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox メッセージの処理中にエラーが発生しました。");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundService が停止されました。");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 未処理のメッセージを取得（最大100件）
        var unprocessedMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!unprocessedMessages.Any())
        {
            return;
        }

        _logger.LogInformation("Outbox メッセージを {Count} 件処理します。", unprocessedMessages.Count);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                // ここでメッセージを実際に配信する処理を実装
                // 例: SignalR Hub への配信、Message Queue への送信など
                await ProcessMessageAsync(message, cancellationToken);

                message.MarkAsProcessed();

                _logger.LogInformation(
                    "Outbox メッセージを処理しました。 [Id: {MessageId}] [Type: {MessageType}]",
                    message.Id,
                    message.Type);
            }
            catch (Exception ex)
            {
                message.MarkAsFailed(ex.Message);

                _logger.LogError(ex,
                    "Outbox メッセージの処理に失敗しました。 [Id: {MessageId}] [Type: {MessageType}] [RetryCount: {RetryCount}]",
                    message.Id,
                    message.Type,
                    message.RetryCount);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        // 実際のメッセージ処理ロジック
        // 現在は単純にログ出力のみ（実運用では SignalR や Message Queue への配信を実装）
        _logger.LogDebug(
            "メッセージ処理中: {Type} - {Content}",
            message.Type,
            message.Content);

        // シミュレーション用の遅延
        await Task.CompletedTask;
    }
}
