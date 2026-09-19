namespace Mooncake.EcommercePlatform.WebApi.Middleware;

using Microsoft.AspNetCore.Diagnostics;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Utils;

/// <summary>
/// Centralized exception handler implementing the .NET 8 <see cref="IExceptionHandler"/> interface.
/// Maps domain and infrastructure exceptions to standardised ApiResponse envelopes.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.Items["TraceId"]?.ToString() ?? httpContext.TraceIdentifier;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        switch (exception)
        {
            case HttpException httpEx:
                logger.LogWarning(httpEx,
                    "Domain error {StatusCode} on {Path} [TraceId={TraceId}]",
                    httpEx.StatusCode, path, traceId);

                httpContext.Response.StatusCode = httpEx.StatusCode;
                await httpContext.Response.WriteAsJsonAsync(
                    ResponseHelper.Error<object>(httpEx.StatusCode, httpEx.Message, path, traceId),
                    cancellationToken);
                break;

            case BadHttpRequestException badRequest:
                logger.LogWarning(badRequest,
                    "Bad request on {Path} [TraceId={TraceId}]", path, traceId);

                var errors = new List<string> { badRequest.Message };
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(
                    ResponseHelper.Error<object>(400, errors, path, traceId),
                    cancellationToken);
                break;

            default:
                logger.LogError(exception,
                    "Unhandled exception on {Path} [TraceId={TraceId}]", path, traceId);

                httpContext.Response.StatusCode = 500;
                await httpContext.Response.WriteAsJsonAsync(
                    ResponseHelper.Error<object>(500, "Internal server error", path, traceId),
                    cancellationToken);
                break;
        }

        return true;
    }
}
