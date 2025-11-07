using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;
using Shared.Infrastructure.Platform.Persistence;

namespace Shared.Infrastructure.Platform.Stores;

/// <summary>
/// Outbox Store の EF Core 実装
///
/// 【Infrastructure.Platform パターン】
///
/// 責務:
/// - Outboxメッセージの永続化（PlatformDbContext使用）
/// - 未処理メッセージの取得
/// - メッセージのステータス更新
///
/// 設計原則:
/// - ポート（IOutboxStore）の具体実装
/// - ビジネスロジックから技術的実装を分離
/// - PlatformDbContextを使用（技術的関心事専用）
/// </summary>
public sealed class OutboxStore : IOutboxStore
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<OutboxStore> _logger;

    public OutboxStore(
        PlatformDbContext context,
        ILogger<OutboxStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        // Shared.Abstractions.Platform.OutboxMessage → Shared.Domain.Outbox.OutboxMessage に変換
        var domainMessage = Shared.Domain.Outbox.OutboxMessage.Create(
            message.Type,
            message.Content);

        await _context.OutboxMessages.AddAsync(domainMessage, cancellationToken);

        _logger.LogDebug(
            "Outboxメッセージを追加しました。[Type: {Type}]",
            message.Type);
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var messages = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("未処理Outboxメッセージを {Count} 件取得しました。", messages.Count);

        // Shared.Domain.Outbox.OutboxMessage → Shared.Abstractions.Platform.OutboxMessage に変換
        return messages.Select(m => new OutboxMessage
        {
            Id = m.Id,
            Type = m.Type,
            Content = m.Content,
            OccurredOnUtc = m.OccurredOnUtc,
            ProcessedOnUtc = m.ProcessedOnUtc,
            Error = m.Error
        });
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Outboxメッセージが見つかりません。[Id: {MessageId}]", messageId);
            return;
        }

        message.MarkAsProcessed();

        _logger.LogDebug("Outboxメッセージを処理済みとしてマークしました。[Id: {MessageId}]", messageId);
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Outboxメッセージが見つかりません。[Id: {MessageId}]", messageId);
            return;
        }

        message.MarkAsFailed(error);

        _logger.LogWarning(
            "Outboxメッセージを失敗としてマークしました。[Id: {MessageId}] [Error: {Error}] [RetryCount: {RetryCount}]",
            messageId,
            error,
            message.RetryCount);
    }
}
