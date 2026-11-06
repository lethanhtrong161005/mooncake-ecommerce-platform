namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Server-authoritative quantity pricing, shipping, tax, deposit, and quote-version preview.</summary>
public sealed record OrderQuotePreviewResponse(
    decimal Subtotal,
    decimal TotalDiscount,
    decimal FinalTotal,
    decimal EffectiveDiscountRate,
    IReadOnlyList<OrderQuoteLineResponse> LineBreakdown)
{
    public string PriceVersion { get; init; } = string.Empty;
    public bool TaxIncluded { get; init; } = true;
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public string? ShippingZoneCode { get; init; }
    public string? ShippingZoneName { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal PayableTotal { get; init; }
    public bool DepositRequired { get; init; }
    public decimal DepositPercent { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal BalanceBeforeShippingAmount { get; init; }
    public decimal VnPayMaxAmountPerPart { get; init; } = 100_000_000m;
    public int EstimatedInitialPaymentParts { get; init; } = 1;
    public IReadOnlyList<ShopShippingRateResponse> AvailableShippingRates { get; init; } = [];
}

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
    AppliedPromotionResponse? AppliedPromotion)
{
    public int AvailableStock { get; init; }
}

/// <summary>Promotion selected by the best-discount engine.</summary>
public sealed record AppliedPromotionResponse(
    Guid Id,
    string Name,
    DiscountType DiscountType,
    decimal DiscountValue);
