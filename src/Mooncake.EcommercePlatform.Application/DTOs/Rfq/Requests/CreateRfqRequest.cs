namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request payload to create a new Request For Quotation (RFQ).</summary>
public record CreateRfqRequest
{
    [Required]
    public long CustomerId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public RfqVisibility Visibility { get; init; } = RfqVisibility.Open;

    public DateTime? QuoteDeadline { get; init; }

    public DateOnly? RequiredDeliveryDate { get; init; }

    [Required]
    public string DeliveryAddress { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<CreateRfqItemRequest> Items { get; init; } = [];

    /// <summary>Optional list of supplier IDs to invite immediately.</summary>
    public List<long>? InvitedSupplierIds { get; init; }
}

public record CreateRfqItemRequest
{
    public long? ProductId { get; init; }

    [Required]
    [MaxLength(255)]
    public string ItemName { get; init; } = string.Empty;

    public string? Specification { get; init; }

    [Required]
    [Range(1, 1000000)]
    public int Quantity { get; init; }

    public long? CustomPackagingId { get; init; }

    public string? Note { get; init; }
}
