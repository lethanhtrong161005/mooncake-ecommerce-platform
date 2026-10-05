namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Supplier-managed quantity-tier promotion.</summary>
public sealed record SavePromotionRequest
{
    [Required, MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    public Guid? ProductId { get; init; }

    [Range(1, 1_000_000)]
    public int MinQty { get; init; }

    [Range(1, 1_000_000)]
    public int? MaxQty { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999")]
    public decimal DiscountValue { get; init; }

    public DiscountType DiscountType { get; init; }

    public DateTimeOffset? StartAtUtc { get; init; }

    public DateTimeOffset? EndAtUtc { get; init; }
}
