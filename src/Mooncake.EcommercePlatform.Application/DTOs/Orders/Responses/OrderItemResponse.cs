namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Details of an individual item in a confirmed order.</summary>
public record OrderItemResponse(
    long Id,
    long OrderId,
    long VariantId,
    string VariantName,
    string? Sku,
    long? PromotionRuleId,
    string? PromotionRuleName,
    long? CustomPackagingId,
    string? CustomPackagingName,
    string? CustomPackagingLogoUrl,
    int Quantity,
    decimal UnitPrice,
    decimal PackagingFee,
    decimal DiscountAmount,
    decimal LineTotal
);
