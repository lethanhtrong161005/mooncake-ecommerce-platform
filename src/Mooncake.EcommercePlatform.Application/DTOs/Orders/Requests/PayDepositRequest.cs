namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to confirm deposit payment for bulk / large orders.</summary>
public record PayDepositRequest
{
    [Required]
    [Range(1, 1000000000)]
    public decimal Amount { get; init; }

    public string? TransactionReference { get; init; }

    public string? Note { get; init; }
}
