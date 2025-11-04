namespace ProductCatalog.Host.Middleware;

/// <summary>
/// リクエストごとに CorrelationID を生成・管理するミドルウェア
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CorrelationIdKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // リクエストヘッダーから CorrelationID を取得、なければ生成
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // HttpContext.Items に保存
        context.Items[CorrelationIdKey] = correlationId;

        // レスポンスヘッダーに追加
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
