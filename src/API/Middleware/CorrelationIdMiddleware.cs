using Infrastructure.Logging;
using Serilog.Context;

namespace API.Middleware;

/// <summary>
/// Ensures every request carries an X-Correlation-Id (generating one if the caller
/// didn't supply it), echoes it back on the response, and pushes it into Serilog's
/// LogContext so every log line for the request can be tied together.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(LoggingConstants.CorrelationIdHeader, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[LoggingConstants.CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty(LoggingConstants.CorrelationIdProperty, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
