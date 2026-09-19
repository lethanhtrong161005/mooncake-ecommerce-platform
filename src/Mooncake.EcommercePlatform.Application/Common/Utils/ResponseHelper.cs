namespace Mooncake.EcommercePlatform.Application.Common.Utils;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

/// <summary>
/// Factory methods that build standardised <see cref="ApiResponse{T}"/> instances.
/// Always pass <paramref name="path"/> and <paramref name="traceId"/> from the request context.
/// </summary>
public static class ResponseHelper
{
    private static ApiResponse<T> BuildSuccess<T>(string message, T? data, string path, string traceId)
    {
        return new ApiResponse<T>
        {
            Status = 200,
            Message = message,
            Data = data,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };
    }

    /// <summary>Returns a 200 response with a typed data payload.</summary>
    public static ApiResponse<T> SuccessWithData<T>(T data, string message, string path = "", string traceId = "") =>
        BuildSuccess(message, data, path, traceId);

    /// <summary>Returns a 200 response with a message only (no payload).</summary>
    public static ApiResponse<object> Success(string message, string path = "", string traceId = "") =>
        BuildSuccess<object>(message, null, path, traceId);

    /// <summary>Returns a 201 Created response with a typed data payload.</summary>
    public static ApiResponse<T> SuccessCreated<T>(T data, string message, string path = "", string traceId = "") =>
        new()
        {
            Status = 201,
            Message = message,
            Data = data,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };

    /// <summary>Returns an error response with a single message.</summary>
    public static ApiResponse<T> Error<T>(int httpStatus, string message, string path = "", string traceId = "") =>
        new()
        {
            Status = httpStatus,
            Message = message,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };

    /// <summary>Returns an error response with a list of validation errors.</summary>
    public static ApiResponse<T> Error<T>(int httpStatus, List<string> errors, string path = "", string traceId = "") =>
        new()
        {
            Status = httpStatus,
            Errors = errors,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };
}
