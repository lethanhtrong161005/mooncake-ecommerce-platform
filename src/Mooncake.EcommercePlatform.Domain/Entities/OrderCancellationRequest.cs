namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a mutual cancellation request submitted when an order cannot be unilaterally cancelled.</summary>
public sealed class OrderCancellationRequest : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public string RequestedByRole { get; set; } = "Customer";

    public string OrderStatusSnapshot { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public decimal ProposedRefundAmount { get; set; }
}

