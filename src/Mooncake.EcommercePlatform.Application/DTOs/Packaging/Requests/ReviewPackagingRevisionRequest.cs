namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer approval or revision request for a packaging mockup.</summary>
public sealed record ReviewPackagingRevisionRequest
{
    [Required(ErrorMessage = "Action is required.")]
    [RegularExpression("^(Approve|RequestRevision)$",
        ErrorMessage = "Action must be either 'Approve' or 'RequestRevision'.")]
    public string Action { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Review notes cannot exceed 2000 characters.")]
    public string? Notes { get; init; }
}
