namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Promotion configuration exposed to its owning supplier.</summary>
public sealed record PromotionResponse(
    Guid Id,
    Guid ShopId,
    Guid? ProductId,
    string Name,
    string? Description,
    int? MinQty,
    int? MaxQty,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    bool IsActive);
