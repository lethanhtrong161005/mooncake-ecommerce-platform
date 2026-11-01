namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Models;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class RfqData
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid CustomerId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateOnly? DeliveryDateRequired { get; set; }
    public decimal? BudgetRangeMin { get; set; }
    public decimal? BudgetRangeMax { get; set; }
    public DateTime? BidDeadline { get; set; }
    public Guid? AwardedBidId { get; set; }
    public DateTime? AwardedAt { get; set; }
    public string? SelectionNote { get; set; }
    public string? CancelReason { get; set; }
    public RfqStatus Status { get; set; }
}

public sealed class RfqItemData
{
    public Guid Id { get; set; }
    public Guid RfqId { get; set; }
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Specifications { get; set; }
    public string? PackagingRequirements { get; set; }
    public bool NeedsCustomPackaging { get; set; }
    public Guid? CreatedBy { get; set; }
}

public sealed class RfqInvitationData
{
    public Guid Id { get; set; }
    public Guid RfqId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? DeclineReason { get; set; }
    public RfqInvitationStatus Status { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class BidData
{
    public Guid Id { get; set; }
    public Guid RfqId { get; set; }
    public Guid SupplierId { get; set; }
    public string BidNumber { get; set; } = string.Empty;
    public decimal? TotalPrice { get; set; }
    public int? EstimatedDeliveryDays { get; set; }
    public string? Notes { get; set; }
    public int ValidityDays { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? CancelReason { get; set; }
    public BidStatus Status { get; set; }
    public Guid? CreatedBy { get; set; }
}

public sealed class BidItemData
{
    public Guid Id { get; set; }
    public Guid BidId { get; set; }
    public Guid RfqItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
}
