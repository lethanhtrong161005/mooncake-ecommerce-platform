namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the system clock. Inject this instead of calling DateTime.UtcNow directly.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Returns the current UTC date and time.</summary>
    DateTime UtcNow { get; }
}
