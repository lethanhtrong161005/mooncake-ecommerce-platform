namespace Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;

/// <summary>Request payload for driver registration after claiming a trip.</summary>
public sealed record DriverRegisterRequest
{
    public string Token { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? VehiclePlate { get; init; }
}
