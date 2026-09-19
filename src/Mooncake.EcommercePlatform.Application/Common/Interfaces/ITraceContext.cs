namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// MDC equivalent: exposes the TraceId bound to the current asynchronous request flow.
/// </summary>
public interface ITraceContext
{
    /// <summary>Unique trace identifier for the current request.</summary>
    string TraceId { get; }
}
