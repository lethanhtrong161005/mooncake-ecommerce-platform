namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to invite a supplier to quote on an RFQ.</summary>
public record InviteSupplierRequest
{
    [Required]
    public long SupplierId { get; init; }
}
