namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the CustomerProfile domain entity.</summary>
public class CustomerProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? TaxCode { get; set; }

    public string? BillingAddress { get; set; }

    public string? ContactPerson { get; set; }
}
