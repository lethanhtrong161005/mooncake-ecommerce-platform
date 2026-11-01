namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Admin;

/// <summary>Explicit, one-time database bootstrap operations.</summary>
public interface IPlatformBootstrapRepository
{
    Task InitializeAsync(PlatformBootstrapData data, CancellationToken cancellationToken = default);
}
