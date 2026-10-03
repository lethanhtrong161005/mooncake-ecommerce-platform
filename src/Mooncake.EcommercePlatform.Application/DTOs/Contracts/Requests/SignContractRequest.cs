namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to digitally sign a contract by Customer or Supplier.</summary>
public record SignContractRequest
{
    [Required]
    public bool IsCustomerSigner { get; init; }

    public string? SignatureNote { get; init; }
}

/// <summary>Request to update contract terms before signing.</summary>
public record UpdateContractTermsRequest
{
    [MaxLength(2000)]
    public string? Terms { get; init; }

    [Range(0, 50)]
    public decimal? LatePenaltyPercentPerDay { get; init; }

    [Range(0, 100)]
    public decimal? MaxPenaltyPercent { get; init; }
}
