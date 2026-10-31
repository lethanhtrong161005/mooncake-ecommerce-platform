namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Models;

using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record PromotionData(Guid Id, Guid ShopId, Guid? ProductId, string Name, string? Description,
    int? MinQty, int? MaxQty, DiscountType DiscountType, decimal DiscountValue, DateTime? StartDateUtc,
    DateTime? EndDateUtc, bool IsActive, Guid? CreatedBy, Guid? UpdatedBy, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
