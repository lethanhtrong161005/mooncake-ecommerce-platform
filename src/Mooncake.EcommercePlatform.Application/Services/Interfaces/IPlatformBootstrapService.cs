namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Explicit command-line initialization of the first admin and marketplace seed data.</summary>
public interface IPlatformBootstrapService
{
    Task InitializeAsync(string adminEmail, string adminPassword, CancellationToken cancellationToken = default);
}
