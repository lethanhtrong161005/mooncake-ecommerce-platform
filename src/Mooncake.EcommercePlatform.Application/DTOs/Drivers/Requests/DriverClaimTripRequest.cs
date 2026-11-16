namespace Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;

/// <summary>Request payload for authenticated driver trip claim.</summary>
public sealed record DriverClaimTripRequest
{
    public string? VehiclePlate { get; init; }
}
