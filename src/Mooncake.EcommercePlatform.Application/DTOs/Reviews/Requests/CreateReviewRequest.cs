namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to submit a customer review for an Order or Contract.</summary>
public record CreateReviewRequest
{
    [Required]
    public long CustomerId { get; init; }

    [Required]
    public long SupplierId { get; init; }

    public long? OrderId { get; init; }

    public long? ContractId { get; init; }

    [Required]
    [Range(1, 5)]
    public short Rating { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}
