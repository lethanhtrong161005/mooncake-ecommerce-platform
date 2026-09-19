namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/>.
/// Always returns <see cref="DateTime.UtcNow"/>.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}
