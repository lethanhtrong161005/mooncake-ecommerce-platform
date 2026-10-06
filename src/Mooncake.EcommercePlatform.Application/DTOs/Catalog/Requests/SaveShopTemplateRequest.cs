namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Admin-managed shop template configuration.</summary>
public sealed record SaveShopTemplateRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Url, MaxLength(1000)]
    public string? PreviewImageUrl { get; init; }

    [MaxLength(4000)]
    public string? CssVariables { get; init; }

    [MaxLength(8000)]
    public string? LayoutConfig { get; init; }

    [Range(0, 100_000)]
    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;

    public bool IsPremium { get; init; }
}
