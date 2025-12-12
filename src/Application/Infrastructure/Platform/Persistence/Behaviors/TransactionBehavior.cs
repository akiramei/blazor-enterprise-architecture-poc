using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Platform.Persistence;

namespace Application.Infrastructure.Platform.Persistence.Behaviors;

/// <summary>
/// PlatformDbContext 用トランザクション管理Behavior（Command専用）
///
/// 目的:
/// - Platform（Identity/認証/監査等）に対するCommandでも
///   Handler内の SaveChangesAsync を禁止し、catalogのNEVER DOを一貫適用する。
///
/// 注意:
/// - Platform層はドメインイベント/Outboxを持たないため、単純なSaveChangesのみ実行する。
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        PlatformDbContext context,
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
        _logger.LogDebug("Platformトランザクション開始: {CommandName}", commandName);

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogDebug("Platformトランザクションコミット: {CommandName}", commandName);
            }
            else
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogDebug("Platformトランザクションロールバック(ビジネスルール違反): {CommandName}", commandName);
            }

            return response;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Platformトランザクションロールバック(例外): {CommandName}", commandName);
            throw;
        }
    }
}

