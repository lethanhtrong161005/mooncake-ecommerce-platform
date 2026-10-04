namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the RequestForQuotation domain entity.</summary>
public class RequestForQuotation : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string RfqNumber { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? DeliveryAddress { get; set; }

    public DateOnly? DeliveryDateRequired { get; set; }

    public decimal? BudgetRangeMin { get; set; }

    public decimal? BudgetRangeMax { get; set; }

    public DateTime? BidDeadline { get; set; }

    public RfqStatus Status { get; set; } = RfqStatus.Draft;
}
