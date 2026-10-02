namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Calculated preview of an individual item.</summary>
public record CalculatedItemResponse(
    long VariantId,
    string VariantName,
    string? Sku,
    long ProductId,
    string ProductName,
    long ShopId,
    string ShopName,
    int Quantity,
    decimal UnitPrice,
    decimal PackagingFee,
    long? PromotionRuleId,
    string? PromotionRuleName,
    decimal DiscountAmount,
    decimal LineTotal,
    long? CustomPackagingId,
    string? CustomPackagingName
);
