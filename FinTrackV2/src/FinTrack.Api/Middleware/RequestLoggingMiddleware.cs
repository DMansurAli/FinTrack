using Serilog.Context;

namespace FinTrack.Api.Middleware;

/// <summary>
/// Enriches every log entry in a request with the authenticated user's ID.
/// This means every log line automatically knows which user triggered it.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value
                  ?? "anonymous";

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}
