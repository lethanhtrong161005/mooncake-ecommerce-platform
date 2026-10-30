namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Supplier mockup submission for customer review.</summary>
public sealed record SubmitPackagingRevisionRequest
{
    [Required(ErrorMessage = "Mockup URL is required.")]
    [Url(ErrorMessage = "Mockup URL must be a valid URL.")]
    public string MockupUrl { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Supplier notes cannot exceed 2000 characters.")]
    public string? SupplierNotes { get; init; }

    [Range(0, 100_000_000, ErrorMessage = "Design fee must be non-negative.")]
    public decimal DesignFee { get; init; }
}
