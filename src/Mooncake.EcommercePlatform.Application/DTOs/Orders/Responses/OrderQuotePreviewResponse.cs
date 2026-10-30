namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Server-authoritative quantity pricing preview.</summary>
public sealed record OrderQuotePreviewResponse(
    decimal Subtotal,
    decimal TotalDiscount,
    decimal FinalTotal,
    decimal EffectiveDiscountRate,
    IReadOnlyList<OrderQuoteLineResponse> LineBreakdown);

/// <summary>Price and promotion breakdown for one quoted variant.</summary>
public sealed record OrderQuoteLineResponse(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    decimal OriginalUnitPrice,
    decimal DiscountedUnitPrice,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal LineTotal,
    AppliedPromotionResponse? AppliedPromotion);

/// <summary>Promotion selected by the best-discount engine.</summary>
public sealed record AppliedPromotionResponse(
    Guid Id,
    string Name,
    DiscountType DiscountType,
    decimal DiscountValue);
