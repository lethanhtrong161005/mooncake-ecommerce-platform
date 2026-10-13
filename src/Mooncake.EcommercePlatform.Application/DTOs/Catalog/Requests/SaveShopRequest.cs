namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Supplier shop details.</summary>
public sealed record SaveShopRequest
{
    [Required, MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), MaxLength(255)]
    public string Slug { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Url, MaxLength(1000)]
    public string? BannerUrl { get; init; }

    [Url, MaxLength(1000)]
    public string? LogoUrl { get; init; }

    [Required]
    public Guid TemplateId { get; init; }
}
