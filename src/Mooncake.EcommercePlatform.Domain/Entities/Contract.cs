namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Contract domain entity.</summary>
public class Contract : BaseEntity
{
    public Guid? RfqId { get; set; }

    public Guid? BidId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid? DepositRuleId { get; set; }

    public string ContractNumber { get; set; } = string.Empty;

    public string? Title { get; set; }

    public decimal? TotalValue { get; set; }

    public DateOnly? DeliveryDeadline { get; set; }

    public string? TermsAndConditions { get; set; }

    public string? PenaltyTerms { get; set; }

    public string? SignedDocumentUrl { get; set; }

    public string? SignedDocumentSha256 { get; set; }

    public decimal? DepositAmount { get; set; }

    public DateTime? DepositPaidAt { get; set; }

    public DateTime? SignedByCustomerAt { get; set; }

    public DateTime? SignedBySupplierAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;
}
