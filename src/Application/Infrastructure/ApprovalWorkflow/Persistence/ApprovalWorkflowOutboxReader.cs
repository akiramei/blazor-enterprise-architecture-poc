using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Platform;

namespace ApprovalWorkflow.Infrastructure.Persistence;

/// <summary>
/// ApprovalWorkflow BC の Outbox Reader
///
/// 【トランザクショナルOutboxパターン - 読み取り実装】
///
/// 責務:
/// - ApprovalWorkflowDbContextのOutboxから未処理メッセージを読み取り
/// - Platform DTO（Shared.Abstractions.Platform.OutboxMessage）への変換
/// - 処理状態の更新（成功/失敗）
///
/// 設計:
/// - 論理所有: Platform（OutboxBackgroundServiceから呼び出される）
/// - 物理実装: ApprovalWorkflow BC
/// - 読み取り専用: 書き込みはTransactionBehaviorが担当
///
/// 使用シナリオ:
/// - ApplicationSubmittedEvent → 承認者へ通知
/// - ApplicationApprovedEvent → 次の承認者または申請者へ通知
/// - ApplicationRejectedEvent → 申請者へ通知
/// - ApplicationReturnedEvent → 申請者へ差し戻し通知
/// </summary>
public sealed class ApprovalWorkflowOutboxReader : IOutboxReader
{
    private readonly ApprovalWorkflowDbContext _context;
    private readonly ILogger<ApprovalWorkflowOutboxReader> _logger;

    public ApprovalWorkflowOutboxReader(
        ApprovalWorkflowDbContext context,
        ILogger<ApprovalWorkflowOutboxReader> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OutboxMessage>> DequeueAsync(int take, CancellationToken cancellationToken = default)
    {
        // 未処理のメッセージを取得（インデックス利用）
        var domainMessages = await _context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Platform DTOに変換
        var messages = domainMessages.Select(m => new OutboxMessage
        {
            Id = m.Id,
            Type = m.Type,
            Content = m.Content,
            OccurredOnUtc = m.OccurredOnUtc
        }).ToList();

        _logger.LogDebug(
            "[ApprovalWorkflow] 未処理Outboxメッセージを {Count} 件取得しました。",
            messages.Count);

        return messages;
    }

    public async Task MarkSucceededAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning(
                "[ApprovalWorkflow] Outboxメッセージが見つかりません。[Id: {MessageId}]",
                id);
            return;
        }

        message.MarkAsProcessed();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[ApprovalWorkflow] Outboxメッセージを処理完了としてマークしました。[Id: {MessageId}] [Type: {Type}]",
            id,
            message.Type);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning(
                "[ApprovalWorkflow] Outboxメッセージが見つかりません。[Id: {MessageId}]",
                id);
            return;
        }

        message.MarkAsFailed(error);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "[ApprovalWorkflow] Outboxメッセージ処理に失敗しました。[Id: {MessageId}] [Type: {Type}] [RetryCount: {RetryCount}] [Error: {Error}]",
            id,
            message.Type,
            message.RetryCount,
            error);
    }
}
