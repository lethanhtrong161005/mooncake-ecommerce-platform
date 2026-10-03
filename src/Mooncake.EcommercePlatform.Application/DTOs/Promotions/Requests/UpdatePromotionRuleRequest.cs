namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to update an existing promotion rule.</summary>
public record UpdatePromotionRuleRequest
{
    public long? ProductId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public DiscountType DiscountType { get; init; }

    [Required]
    [Range(1, 100000)]
    public int MinQuantity { get; init; }

    [Range(0.01, 100)]
    public decimal? DiscountPercent { get; init; }

    [Range(0, 100000000)]
    public decimal? DiscountAmount { get; init; }

    [Range(1, 10000)]
    public int? FreeQuantity { get; init; }

    public DateTime? StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }

    public bool IsActive { get; init; } = true;
}
