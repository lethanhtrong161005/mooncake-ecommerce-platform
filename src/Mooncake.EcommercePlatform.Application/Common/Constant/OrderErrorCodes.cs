namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Standardized machine-readable error codes for enterprise bulk order, payment, packaging, and delivery workflows.</summary>
public static class OrderErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string PriceChanged = "PRICE_CHANGED";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string ShopUnavailable = "SHOP_UNAVAILABLE";
    public const string OrderExpired = "ORDER_EXPIRED";
    public const string StateConflict = "STATE_CONFLICT";
    public const string PaymentRequired = "PAYMENT_REQUIRED";
    public const string PaymentMethodNotSupported = "PAYMENT_METHOD_NOT_SUPPORTED";
    public const string PackagingApprovalRequired = "PACKAGING_APPROVAL_REQUIRED";
    public const string PackagingNotSupported = "PACKAGING_NOT_SUPPORTED";
    public const string DeliveryProofRequired = "DELIVERY_PROOF_REQUIRED";
    public const string SingleDeliveryLimit = "SINGLE_DELIVERY_LIMIT";
    public const string TenantForbidden = "TENANT_FORBIDDEN";
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string FeatureDisabled = "FEATURE_DISABLED";
}

