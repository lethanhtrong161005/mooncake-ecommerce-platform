namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents driver profile information.</summary>
public class DriverProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? DefaultVehiclePlate { get; set; }
}
