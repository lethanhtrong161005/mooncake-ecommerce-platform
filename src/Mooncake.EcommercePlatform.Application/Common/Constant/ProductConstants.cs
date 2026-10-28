namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Stable catalog operation names, limits, errors and public messages.</summary>
public static class ProductConstants
{
    public const string Success = "Product catalog request completed successfully.";
    public const string Error = "The product catalog request could not be completed.";
    public const string Invalid = "PRODUCT_REQUEST_INVALID";
    public const string NotEligible = "SUPPLIER_NOT_ELIGIBLE";
    public const string ShopRequired = "SHOP_REQUIRED";
    public const string ShopInactive = "SHOP_INACTIVE";
    public const string NotFound = "PRODUCT_NOT_FOUND";
    public const string VariantNotFound = "PRODUCT_VARIANT_NOT_FOUND";
    public const string VersionConflict = "PRODUCT_VERSION_CONFLICT";
    public const string KeyConflict = "PRODUCT_IDEMPOTENCY_CONFLICT";
    public const string SkuConflict = "PRODUCT_SKU_CONFLICT";
    public const string VariantRequired = "PRODUCT_VARIANT_REQUIRED";
    public const string PriceInvalid = "PRODUCT_PRICE_INVALID";
    public const string GalleryInvalid = "PRODUCT_GALLERY_INVALID";
    public const string PublicationIncomplete = "PRODUCT_PUBLICATION_INCOMPLETE";
    public const string InventoryInsufficient = "PRODUCT_INVENTORY_INSUFFICIENT";
    public const string MediaNotReady = "PRODUCT_MEDIA_NOT_READY";
    public const string MediaConflict = "PRODUCT_MEDIA_ALREADY_BOUND";
    public const string CursorInvalid = "PRODUCT_CURSOR_INVALID";
    public const string RateLimited = "PRODUCT_RATE_LIMITED";
    public const string Busy = "PRODUCT_BUSY";
    public const string SearchUnavailable = "PRODUCT_SEARCH_UNAVAILABLE";
    public const string RatePolicy = "product-catalog";
    public const string Create = "ProductCreated";
    public const string Update = "ProductCoreUpdated";
    public const string VariantCreate = "ProductVariantCreated";
    public const string VariantUpdate = "ProductVariantUpdated";
    public const string VariantDelete = "ProductVariantDeleted";
    public const string InventoryAdjust = "ProductInventoryAdjusted";
    public const string GalleryUpdate = "ProductGalleryUpdated";
    public const string Publication = "ProductPublicationChanged";
    public const string Delete = "ProductDeleted";
    public const int MaxVariants = 100;
    public const int MaxImages = 10;
    public const int MaxMediaBytes = 5 * 1024 * 1024;
}
