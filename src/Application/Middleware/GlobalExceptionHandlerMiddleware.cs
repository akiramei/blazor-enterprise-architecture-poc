using System.Net;
using System.Text.Json;
using FluentValidation;
using Shared.Application.Interfaces;

namespace Application.Middleware;

/// <summary>
/// グローバル例外ハンドリングミドルウェア
/// すべてのキャッチされない例外を処理し、適切なエラーレスポンスを返す
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = correlationIdAccessor.CorrelationId;

            _logger.LogError(ex,
                "未処理の例外が発生しました。 [CorrelationId: {CorrelationId}] [Path: {Path}]",
                correlationId,
                context.Request.Path);

            try
            {
                await HandleExceptionAsync(context, ex, correlationId, _logger);
            }
            catch (Exception handlerEx)
            {
                // エラーハンドリング自体が失敗した場合の最終防衛策
                _logger.LogError(handlerEx,
                    "エラーハンドリング中に例外が発生しました。 [CorrelationId: {CorrelationId}] [Path: {Path}]",
                    correlationId,
                    context.Request.Path);

                // レスポンスがまだ開始されていない場合のみ、安全なフォールバックレスポンスを返す
                if (!context.Response.HasStarted)
                {
                    try
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";

                        // 最小限のフォールバックエラーレスポンス
                        var fallbackResponse = JsonSerializer.Serialize(new
                        {
                            statusCode = 500,
                            message = "サーバー内部でエラーが発生しました。",
                            correlationId = correlationId,
                            timestamp = DateTime.UtcNow
                        }, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        await context.Response.WriteAsync(fallbackResponse);
                    }
                    catch
                    {
                        // 最終フォールバックも失敗した場合は何もしない（ログは既に記録済み）
                        // レスポンスの送信を試みて失敗しても、パイプラインをクラッシュさせない
                    }
                }
                // レスポンスが既に開始されている場合は何もできない（ログのみ記録）
            }
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string? correlationId,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        // レスポンスが既に開始されている場合は、ヘッダーやボディを変更できないため
        // 何もせずに戻る（ストリーミングレスポンスなどで発生する可能性がある）
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "例外が発生しましたが、レスポンスは既に開始されているため、エラーレスポンスを送信できません。 " +
                "[CorrelationId: {CorrelationId}] [Path: {Path}] [Exception: {ExceptionType}]",
                correlationId,
                context.Request.Path,
                exception.GetType().Name);
            return;
        }

        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                FormatValidationErrors(validationEx)),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Invalid request"),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "認証が必要です。"),

            _ => (
                HttpStatusCode.InternalServerError,
                "サーバー内部でエラーが発生しました。")
        };

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }

    private static string FormatValidationErrors(ValidationException validationException)
    {
        var errors = validationException.Errors
            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}");

        return string.Join("; ", errors);
    }
}

/// <summary>
/// エラーレスポンス
/// </summary>
public sealed record ErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; }
}
