namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

public interface IProductCatalogService
{
    Task<ProductPageResponse<ProductOwnerCardResponse>> ListMineAsync(Guid actor, OwnerProductQuery query, CancellationToken ct);
    Task<ProductOwnerResponse> GetMineAsync(Guid actor, Guid productId, CancellationToken ct);
    Task<ProductOwnerResponse> CreateAsync(Guid actor, string key, CreateProductRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> UpdateAsync(Guid actor, Guid productId, string key, UpdateProductCoreRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> AddVariantAsync(Guid actor, Guid productId, string key, CreateProductVariantRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> UpdateVariantAsync(Guid actor, Guid productId, Guid variantId, string key, UpdateProductVariantRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> DeleteVariantAsync(Guid actor, Guid productId, Guid variantId, long expectedVersion, string key, CancellationToken ct);
    Task<InventoryAdjustmentResponse> AdjustInventoryAsync(Guid actor, Guid productId, Guid variantId, string key, AdjustVariantInventoryRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> ReplaceGalleryAsync(Guid actor, Guid productId, string key, ReplaceProductGalleryRequest request, CancellationToken ct);
    Task<ProductOwnerResponse> SetPublicationAsync(Guid actor, Guid productId, string key, SetProductPublicationRequest request, CancellationToken ct);
    Task<DeleteProductResponse> DeleteAsync(Guid actor, Guid productId, long expectedVersion, string key, CancellationToken ct);
    Task<ShopMediaResponse> UploadMediaAsync(Guid actor, string key, string purpose, Stream content, CancellationToken ct);
    Task<ProductPublicDetailResponse> PublicAsync(Guid productId, CancellationToken ct);
    Task<ProductSearchResponse> SearchAsync(ProductSearchQuery query, CancellationToken ct);
    Task<IReadOnlyList<ProductAutocompleteResponse>> AutocompleteAsync(ProductAutocompleteQuery query, CancellationToken ct);
    Task<IReadOnlyList<ProductSearchCardResponse>> RelatedAsync(Guid productId, int limit, Guid? userId, CancellationToken ct);
    Task<IReadOnlyList<ProductSearchCardResponse>> RecommendationsAsync(int limit, Guid? userId, CancellationToken ct);
    Task RecordViewAsync(Guid productId, Guid? userId, RecordProductViewRequest request, CancellationToken ct);
    Task<IReadOnlyList<CategoryCatalogResponse>> CategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<RegionCatalogResponse>> RegionsAsync(CancellationToken ct);
}
