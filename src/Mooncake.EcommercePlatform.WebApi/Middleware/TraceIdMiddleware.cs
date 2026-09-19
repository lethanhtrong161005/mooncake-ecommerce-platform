namespace Mooncake.EcommercePlatform.WebApi.Middleware;

/// <summary>
/// First middleware in the ASP.NET Core pipeline.
/// Extracts or generates a unique TraceId, stores it in HttpContext.Items,
/// sets the X-Trace-Id response header, and enriches the ILogger scope (MDC equivalent).
/// </summary>
public class TraceIdMiddleware(RequestDelegate next, ILogger<TraceIdMiddleware> logger)
{
    private const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
                      ?? Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        context.Response.Headers[TraceIdHeader] = traceId;

        // MDC equivalent: Attach TraceId to all subsequent logs in this request scope.
        using (logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
        {
            await next(context);
        }
    }
}
