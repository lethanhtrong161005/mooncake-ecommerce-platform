namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to update shop details.</summary>
public record UpdateShopRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [MaxLength(1000)]
    public string? LogoUrl { get; init; }

    [MaxLength(1000)]
    public string? BannerUrl { get; init; }

    public bool IsActive { get; init; } = true;
}
