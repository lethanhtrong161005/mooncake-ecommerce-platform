namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the SupplierProfile domain entity.</summary>
public class SupplierProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Description { get; set; }

    public decimal ReputationScore { get; set; }

    public int TotalContracts { get; set; }

    public int ContractsOnTime { get; set; }

    public int ContractsLate { get; set; }

    public int ContractsBreached { get; set; }

    public decimal? AvgRating { get; set; }

    public bool Verified { get; set; }

    public DateTime? VerifiedAt { get; set; }
}
