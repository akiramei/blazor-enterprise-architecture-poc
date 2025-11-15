using System.Net;
using System.Text.Json;
using FluentValidation;
using Shared.Application.Interfaces;

namespace Application.Host.Middleware;

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

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string? correlationId)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                FormatValidationErrors(validationEx)),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                exception.Message),

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
