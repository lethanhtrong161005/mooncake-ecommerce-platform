namespace Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to update an existing shop template.</summary>
public record UpdateShopTemplateRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [MaxLength(1000)]
    public string? PreviewUrl { get; init; }

    public string Config { get; init; } = "{}";

    public bool IsActive { get; init; }
}
