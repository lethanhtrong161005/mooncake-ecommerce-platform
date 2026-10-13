namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Represents a recurring background task scheduled by the API host.</summary>
public interface IRecurringJob
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}
