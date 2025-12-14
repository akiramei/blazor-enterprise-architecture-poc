using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Domain.Outbox;
using Shared.Kernel;

namespace Application.Infrastructure.LibraryManagement.Persistence.Behaviors;

/// <summary>
/// LibraryManagement BCのトランザクション管理Behavior（Command専用）
///
/// Transactional Outbox Pattern:
/// - ドメインイベントをOutboxMessagesに保存
/// - 単一トランザクションで原子性を確保
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly LibraryManagementDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        LibraryManagementDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var commandName = typeof(TRequest).Name;
        _logger.LogDebug("LibraryManagementトランザクション開始: {CommandName}", commandName);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                await DispatchDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogDebug("LibraryManagementトランザクションコミット: {CommandName}", commandName);
            }
            else
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogDebug("LibraryManagementトランザクションロールバック(ビジネスルール違反): {CommandName}", commandName);
            }

            return response;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "LibraryManagementトランザクションロールバック(例外): {CommandName}", commandName);
            throw;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.GetDomainEvents().Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();

        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var eventContent = JsonSerializer.Serialize(domainEvent, eventType);
            var outboxMessage = OutboxMessage.Create(eventType.Name, eventContent);
            await _context.OutboxMessages.AddAsync(outboxMessage, ct);
        }
    }
}

