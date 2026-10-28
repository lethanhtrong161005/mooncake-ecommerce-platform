namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Stable shop operation names, error codes and public messages.</summary>
public static class ShopConstants
{
    public const string Success = "Shop request completed successfully.";
    public const string Error = "The shop request could not be completed.";
    public const string Invalid = "SHOP_INVALID_REQUEST";
    public const string Forbidden = "SHOP_OPERATION_FORBIDDEN";
    public const string NotVerified = "SUPPLIER_NOT_VERIFIED";
    public const string NotFound = "SHOP_NOT_FOUND";
    public const string Unavailable = "SHOP_UNAVAILABLE";
    public const string Exists = "SHOP_ALREADY_EXISTS";
    public const string SlugInUse = "SHOP_SLUG_IN_USE";
    public const string Conflict = "SHOP_VERSION_CONFLICT";
    public const string Suspended = "SHOP_SUSPENDED";
    public const string StateConflict = "SHOP_STATE_CONFLICT";
    public const string KeyMismatch = "SHOP_IDEMPOTENCY_PAYLOAD_MISMATCH";
    public const string TemplateUnavailable = "TEMPLATE_NOT_SELECTABLE";
    public const string RevisionChanged = "TEMPLATE_REVISION_CHANGED";
    public const string TemplateConflict = "TEMPLATE_VERSION_CONFLICT";
    public const string TemplateMinimum = "TEMPLATE_MINIMUM_CATALOG";
    public const string DefaultRequired = "TEMPLATE_DEFAULT_REQUIRED";
    public const string TemplateCodeInUse = "TEMPLATE_CODE_IN_USE";
    public const string ThemeInvalid = "THEME_TOKENS_INVALID";
    public const string ContrastInvalid = "THEME_CONTRAST_INVALID";
    public const string CursorInvalid = "SHOP_CURSOR_INVALID";
    public const string MediaNotReady = "SHOP_MEDIA_NOT_READY";
    public const string MediaUnsupported = "SHOP_MEDIA_UNSUPPORTED";
    public const string MediaTooLarge = "SHOP_MEDIA_TOO_LARGE";
    public const string StorageUnavailable = "SHOP_MEDIA_STORAGE_UNAVAILABLE";
    public const string RateLimited = "SHOP_RATE_LIMITED";
    public const string Busy = "SHOP_BUSY";
    public const string RatePolicy = "shop-management";
    public const string Create = "ShopCreated";
    public const string Update = "ShopInformationUpdated";
    public const string Visibility = "ShopVisibilityChanged";
    public const string Appearance = "ShopAppearanceChanged";
    public const string Moderation = "ShopModerated";
    public const string TemplateCreate = "ShopTemplateCreated";
    public const string TemplatePublish = "ShopTemplateRevisionPublished";
    public const string Availability = "ShopTemplateAvailabilityChanged";
    public const string Premium = "ShopTemplatePremiumChanged";
    public const int MaxMediaBytes = 5 * 1024 * 1024;
    public const string MediaFailureLog = "Shop media processing failed. Stage={Stage} FailureType={FailureType}";
    public const string MediaAttempt = "ShopMediaUploadAttempted";
    public const string MediaReady = "ShopMediaReady";
}
