using Ease_HRM.Application.Common.Exceptions;

namespace Ease_HRM.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled Exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (exception is ConcurrencyException concurrencyException)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status409Conflict;

            var conflictResponse = new
            {
                message = "The record was modified by another user.",
                code = "CONCURRENCY_CONFLICT",
                entity = concurrencyException.EntityName ?? "Unknown"
            };

            return context.Response.WriteAsJsonAsync(conflictResponse, cancellationToken: context.RequestAborted);
        }

        context.Response.ContentType = "application/json";

        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var message = statusCode == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        var response = new
        {
            success = false,
            message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
    }
}