namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;

/// <summary>Maps persistence snapshots to owner catalog responses without exposing entities.</summary>
public static class ProductResponseHelper
{
    public static ProductOwnerResponse Owner(ProductStateDto state, bool replay = false)
    {
        var status = ProductPolicyHelper.Effective(state.IsPublished, state.Variants);
        var variants = state.Variants.Where(x => !x.IsDeleted).OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Select(x => new ProductVariantOwnerResponse(x.Id, x.Name, x.Sku, x.CrustType, x.Filling,
                x.SizeLabel, x.WeightGram, x.IsGiftSet, x.PriceAdjustment,
                ProductPolicyHelper.Price(state.BasePrice, x.PriceAdjustment), x.StockQty, x.InventoryVersion,
                x.ImageAssetId, x.ImageUrl)).ToArray();
        var images = state.Images.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new ProductImageResponse(x.Id, x.MediaAssetId, x.ImageUrl, x.IsPrimary, x.SortOrder)).ToArray();
        var issues = new List<string>();
        if (!state.CategoryActive) issues.Add("CATEGORY_UNAVAILABLE");
        if (variants.Length == 0) issues.Add("VARIANT_REQUIRED");
        if (images.Length == 0) issues.Add("IMAGE_REQUIRED");
        if (images.Count(x => x.IsPrimary) != 1) issues.Add("PRIMARY_IMAGE_REQUIRED");
        if (state.Images.Any(x => !x.IsDeleted && !x.MediaReady)) issues.Add("MEDIA_NOT_READY");
        return new(state.Id, state.ShopId, state.Version, state.Name, state.Description, state.Ingredients, state.CategoryId,
            state.BasePrice, state.MinOrderQty, state.MaxOrderQty, state.Unit, state.SupportsCustomPackaging,
            state.IsPublished, status, variants, images, issues, state.CreatedAtUtc, state.UpdatedAtUtc, replay);
    }

    public static T Replay<T>(T response) where T : notnull => response switch
    {
        ProductOwnerResponse owner => (T)(object)(owner with { IdempotentReplay = true }),
        InventoryAdjustmentResponse inventory => (T)(object)(inventory with { IdempotentReplay = true }),
        DeleteProductResponse deleted => (T)(object)(deleted with { IdempotentReplay = true }),
        _ => response
    };
}
