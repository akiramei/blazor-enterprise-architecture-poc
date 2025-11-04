using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Domain.Idempotency;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// 冪等性保証のPipeline Behavior（Command専用）
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyStore store,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Commandからキーを取得
        var idempotencyKey = GetIdempotencyKey(request);

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return await next();  // キーがない場合はスキップ
        }

        var commandType = typeof(TRequest).Name;

        // 既に処理済みかチェック
        var existingRecord = await _store.GetAsync(idempotencyKey, cancellationToken);

        if (existingRecord != null)
        {
            _logger.LogInformation(
                "冪等性により既存の結果を返します: {CommandType} [Key: {IdempotencyKey}]",
                commandType,
                idempotencyKey);

            return existingRecord.GetResult<TResponse>();
        }

        // 新規処理を実行
        var response = await next();

        // 成功した場合のみ記録
        if (response.IsSuccess)
        {
            var record = IdempotencyRecord.Create(idempotencyKey, commandType, response);
            await _store.SaveAsync(record, cancellationToken);

            _logger.LogInformation(
                "冪等性レコードを保存しました: {CommandType} [Key: {IdempotencyKey}]",
                commandType,
                idempotencyKey);
        }

        return response;
    }

    private string? GetIdempotencyKey(TRequest request)
    {
        var property = typeof(TRequest).GetProperty("IdempotencyKey");
        return property?.GetValue(request) as string;
    }
}
