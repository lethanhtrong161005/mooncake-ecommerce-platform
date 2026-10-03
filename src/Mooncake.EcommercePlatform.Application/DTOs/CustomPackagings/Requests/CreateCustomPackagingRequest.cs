namespace Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to design custom gift packaging with company/personal logo.</summary>
public record CreateCustomPackagingRequest
{
    [Required]
    public long CustomerId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string LogoUrl { get; init; } = string.Empty;

    public string? DesignNotes { get; init; }
}
