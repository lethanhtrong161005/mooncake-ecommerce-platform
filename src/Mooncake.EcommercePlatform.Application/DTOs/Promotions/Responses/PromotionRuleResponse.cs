namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Tiered or volume promotion rule projection.</summary>
public record PromotionRuleResponse(
    long Id,
    long ShopId,
    long? ProductId,
    string Name,
    DiscountType DiscountType,
    int MinQuantity,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    int? FreeQuantity,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool IsActive
);
