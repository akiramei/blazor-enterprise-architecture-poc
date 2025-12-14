namespace Application.Middleware;

/// <summary>
/// 繝ｪ繧ｯ繧ｨ繧ｹ繝医＃縺ｨ縺ｫ CorrelationID 繧堤函謌舌・邂｡逅・☆繧九Α繝峨Ν繧ｦ繧ｧ繧｢
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
        // 繝ｪ繧ｯ繧ｨ繧ｹ繝医・繝・ム繝ｼ縺九ｉ CorrelationID 繧貞叙蠕励√↑縺代ｌ縺ｰ逕滓・
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // HttpContext.Items 縺ｫ菫晏ｭ・
        context.Items[CorrelationIdKey] = correlationId;

        // 繝ｬ繧ｹ繝昴Φ繧ｹ繝倥ャ繝繝ｼ縺ｫ霑ｽ蜉
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
