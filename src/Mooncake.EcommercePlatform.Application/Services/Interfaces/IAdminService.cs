namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Admin.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Admin.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Responses;

/// <summary>Contract for administration services (supplier verification, template and category management).</summary>
public interface IAdminService
{
    Task<IEnumerable<SupplierResponse>> GetSuppliersAsync(bool? isVerified = null, CancellationToken cancellationToken = default);
    Task<SupplierResponse> VerifySupplierAsync(long supplierId, VerifySupplierRequest request, CancellationToken cancellationToken = default);

    Task<IEnumerable<ShopTemplateResponse>> GetShopTemplatesAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<ShopTemplateResponse> GetShopTemplateByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShopTemplateResponse> CreateShopTemplateAsync(CreateShopTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ShopTemplateResponse> UpdateShopTemplateAsync(long id, UpdateShopTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteShopTemplateAsync(long id, CancellationToken cancellationToken = default);

    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateCategoryAsync(long id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(long id, CancellationToken cancellationToken = default);
}
