namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Supplier business details and verification file references.</summary>
public sealed record ApplySupplierRequest
{
    [Required, MaxLength(255)]
    public string CompanyName { get; init; } = string.Empty;

    [Required, MaxLength(50)]
    public string TaxCode { get; init; } = string.Empty;

    [Required, MaxLength(500)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required, MinLength(1), MaxLength(10)]
    public List<VerificationDocumentRequest> Documents { get; init; } = [];
}

/// <summary>Reference to a previously uploaded verification document.</summary>
public sealed record VerificationDocumentRequest
{
    [Required, MaxLength(50)]
    public string DocumentType { get; init; } = string.Empty;

    [Required, Url, MaxLength(1000)]
    public string FileUrl { get; init; } = string.Empty;

    [MaxLength(255)]
    public string? OriginalFileName { get; init; }

    [MaxLength(100)]
    public string? ContentType { get; init; }

    [Range(1, 25_000_000)]
    public long? FileSizeBytes { get; init; }
}
