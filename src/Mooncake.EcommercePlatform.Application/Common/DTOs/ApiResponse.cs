namespace Mooncake.EcommercePlatform.Application.Common.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Standard API response envelope returned by every endpoint.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>HTTP status code mirrored in the body.</summary>
    public int Status { get; set; }

    /// <summary>Human-readable success or error message.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>Payload returned on success.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>Validation or business-rule error list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    /// <summary>Request path, e.g. "/api/v1/users".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Unique trace identifier propagated through the MDC / ILogger scope.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this response was produced.</summary>
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
