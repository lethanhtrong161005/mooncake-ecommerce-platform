namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

public interface IShopService
{
    Task<OwnerShopResponse> GetMineAsync(Guid actor, CancellationToken ct);
    Task<OwnerShopResponse> CreateAsync(Guid actor, string key, CreateShopRequest request, CancellationToken ct);
    Task<OwnerShopResponse> UpdateAsync(Guid actor, string key, UpdateShopRequest request, CancellationToken ct);
    Task<OwnerShopResponse> VisibilityAsync(Guid actor, string key, SetMyShopVisibilityRequest request, CancellationToken ct);
    Task<OwnerShopResponse> AppearanceAsync(Guid actor, string key, UpdateShopAppearanceRequest request, CancellationToken ct);
    Task<OwnerShopResponse> ModerateAsync(Guid actor, Guid shopId, string key, AdminShopModerationRequest request, CancellationToken ct);
    Task<ShopPageResponse<AdminShopSummaryResponse>> ListAdminAsync(Guid actor, AdminShopQuery query, CancellationToken ct);
    Task<OwnerShopResponse> DetailAdminAsync(Guid actor, Guid shopId, CancellationToken ct);
    Task<OwnerShopPreviewResponse> PreviewAsync(Guid actor, CancellationToken ct);
    Task<PublicShopResponse> PublicAsync(string slug, CancellationToken ct);
    Task<ShopCursorPageResponse<ShopProductCardResponse>> ProductsAsync(string slug, ShopProductQuery query, CancellationToken ct);
    Task<ShopCursorPageResponse<ShopCategoryResponse>> CategoriesAsync(string slug, ShopPageQuery query, CancellationToken ct);
    Task<ShopCursorPageResponse<ShopReviewResponse>> ReviewsAsync(string slug, ShopReviewQuery query, CancellationToken ct);
    Task<ShopCursorPageResponse<ShopTemplateResponse>> CatalogAsync(Guid actor, ShopPageQuery query, CancellationToken ct);
    Task<ShopPageResponse<ShopTemplateResponse>> TemplatesAdminAsync(Guid actor, AdminTemplateQuery query, CancellationToken ct);
    Task<ShopTemplateResponse> TemplateDetailAsync(Guid actor, Guid templateId, CancellationToken ct);
    Task<ShopTemplateResponse> CreateTemplateAsync(Guid actor, string key, CreateShopTemplateRequest request, CancellationToken ct);
    Task<ShopTemplateResponse> PublishTemplateAsync(Guid actor, Guid templateId, string key, PublishTemplateRevisionRequest request, CancellationToken ct);
    Task<ShopTemplateResponse> TemplateAvailabilityAsync(Guid actor, Guid templateId, string key, SetTemplateAvailabilityRequest request, CancellationToken ct);
    Task<ShopTemplateResponse> TemplatePremiumAsync(Guid actor, Guid templateId, string key, SetTemplatePremiumRequest request, CancellationToken ct);
    Task<ShopMediaResponse> UploadAsync(Guid actor, bool administrator, string key, string purpose, Stream content, CancellationToken ct);
}
