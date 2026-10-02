namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;

/// <summary>Contract for Shop storefront management and configuration.</summary>
public interface IShopService
{
    Task<IEnumerable<ShopResponse>> GetActiveShopsAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<ShopDetailResponse> GetShopByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShopDetailResponse> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ShopResponse?> GetShopBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<ShopResponse> CreateShopAsync(CreateShopRequest request, CancellationToken cancellationToken = default);
    Task<ShopResponse> UpdateShopAsync(long id, UpdateShopRequest request, CancellationToken cancellationToken = default);
    Task<ShopResponse> ApplyTemplateAsync(long id, ApplyShopTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteShopAsync(long id, CancellationToken cancellationToken = default);
}
