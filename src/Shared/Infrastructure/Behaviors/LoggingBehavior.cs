using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// ログ出力のPipeline Behavior
/// すべてのリクエストの実行を記録
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();
        var correlationId = _correlationIdAccessor.CorrelationId;

        _logger.LogInformation(
            "処理開始: {RequestName} {@Request} [RequestId: {RequestId}] [CorrelationId: {CorrelationId}]",
            requestName,
            request,
            requestId,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "処理完了: {RequestName} [RequestId: {RequestId}] [CorrelationId: {CorrelationId}] 実行時間: {ElapsedMs}ms",
                requestName,
                requestId,
                correlationId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "処理失敗: {RequestName} [RequestId: {RequestId}] [CorrelationId: {CorrelationId}] 実行時間: {ElapsedMs}ms",
                requestName,
                requestId,
                correlationId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
