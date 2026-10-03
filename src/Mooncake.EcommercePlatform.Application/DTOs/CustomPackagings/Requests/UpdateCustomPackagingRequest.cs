namespace Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to update an existing custom packaging design.</summary>
public record UpdateCustomPackagingRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string LogoUrl { get; init; } = string.Empty;

    public string? DesignNotes { get; init; }
}
