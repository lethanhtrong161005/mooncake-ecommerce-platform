namespace Mooncake.EcommercePlatform.Application.Common.Exceptions;

/// <summary>
/// Domain-level exception that carries an HTTP status code, optional error code, and optional structured payload.
/// Throw from services to signal expected, client-facing error conditions.
/// </summary>
public sealed class HttpException(
    int statusCode,
    string message,
    string? errorCode = null,
    object? errorData = null) : Exception(message)
{
    public string? ErrorCode { get; } = errorCode;

    /// <summary>HTTP status code to return to the caller.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Optional structured payload returned in ApiResponse.Data for conflict recovery.</summary>
    public object? ErrorData { get; } = errorData;
}
