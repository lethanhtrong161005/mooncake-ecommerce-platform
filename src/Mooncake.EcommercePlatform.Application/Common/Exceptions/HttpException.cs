namespace Mooncake.EcommercePlatform.Application.Common.Exceptions;

/// <summary>
/// Domain-level exception that carries an HTTP status code.
/// Throw from services to signal expected, client-facing error conditions.
/// </summary>
public sealed class HttpException(int statusCode, string message, string? errorCode = null) : Exception(message)
{
    public string? ErrorCode { get; } = errorCode;
    /// <summary>HTTP status code to return to the caller.</summary>
    public int StatusCode { get; } = statusCode;
}
