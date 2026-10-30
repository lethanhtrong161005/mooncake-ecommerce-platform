namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer branding and packaging requirements for an order.</summary>
public sealed record CreateCustomPackagingRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(255, MinimumLength = 2,
        ErrorMessage = "Company name must be between 2 and 255 characters.")]
    public string CompanyName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company logo URL is required.")]
    [Url(ErrorMessage = "Company logo URL must be a valid URL.")]
    public string CompanyLogoUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Packaging type is required.")]
    [StringLength(100, ErrorMessage = "Packaging type cannot exceed 100 characters.")]
    public string PackagingType { get; init; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Color scheme JSON cannot exceed 1000 characters.")]
    public string? ColorScheme { get; init; }

    [MaxLength(500, ErrorMessage = "Special message cannot exceed 500 characters.")]
    public string? SpecialMessage { get; init; }

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    public string? Notes { get; init; }
}
