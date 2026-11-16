namespace Mooncake.EcommercePlatform.Application.DTOs.Drivers.Responses;

/// <summary>Driver profile details response.</summary>
public sealed record DriverProfileResponse(
    Guid UserId,
    string FullName,
    string Phone,
    string? DefaultVehiclePlate,
    DateTime CreatedAt);
