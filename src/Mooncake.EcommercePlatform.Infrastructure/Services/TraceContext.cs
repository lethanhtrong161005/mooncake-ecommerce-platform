namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// Reads the TraceId stored by TraceIdMiddleware from the current HTTP context.
/// Implements the MDC equivalent for the application layer.
/// </summary>
public class TraceContext(IHttpContextAccessor httpContextAccessor) : ITraceContext
{
    /// <inheritdoc/>
    public string TraceId =>
        httpContextAccessor.HttpContext?.Items["TraceId"]?.ToString()
        ?? httpContextAccessor.HttpContext?.TraceIdentifier
        ?? string.Empty;
}
