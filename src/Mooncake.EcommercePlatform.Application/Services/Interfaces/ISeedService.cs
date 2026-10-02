namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Contract for demo data seeding across all 30 database tables.</summary>
public interface ISeedService
{
    Task<string> SeedDemoDataAsync(CancellationToken cancellationToken = default);
}
