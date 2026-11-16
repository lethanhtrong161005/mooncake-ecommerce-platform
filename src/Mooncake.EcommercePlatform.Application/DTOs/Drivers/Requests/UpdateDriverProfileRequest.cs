namespace Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;

/// <summary>Request payload for updating driver profile information.</summary>
public sealed record UpdateDriverProfileRequest
{
    public string FullName { get; init; } = string.Empty;

    public string? DefaultVehiclePlate { get; init; }
}
