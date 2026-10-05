namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Admin decision for a supplier verification application.</summary>
public sealed record ReviewSupplierRequest
{
    [Required]
    public bool Approve { get; init; }

    [MaxLength(1000)]
    public string? RejectionReason { get; init; }
}
