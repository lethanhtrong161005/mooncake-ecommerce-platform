namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the CustomPackaging domain entity.</summary>
public class CustomPackaging : BaseEntity
{
    public Guid OrderId { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? BoxDesignUrl { get; set; }

    public string? PackagingType { get; set; }

    public string? ColorScheme { get; set; }

    public string? SpecialMessage { get; set; }

    public string? Notes { get; set; }

    public CustomPackagingStatus Status { get; set; } = CustomPackagingStatus.Pending;
}
