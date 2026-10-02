namespace Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to register/create a new storefront for a supplier.</summary>
public record CreateShopRequest
{
    [Required]
    public long SupplierId { get; init; }

    public long? TemplateId { get; init; }

    public string TemplateOverrides { get; init; } = "{}";

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }

    [MaxLength(1000)]
    public string? LogoUrl { get; init; }

    [MaxLength(1000)]
    public string? BannerUrl { get; init; }
}
