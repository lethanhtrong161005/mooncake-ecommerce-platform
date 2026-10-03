namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements shop and storefront management logic.</summary>
public class ShopService(
    IShopRepository shopRepository,
    ISupplierRepository supplierRepository,
    IShopTemplateRepository shopTemplateRepository,
    IShopHelper shopHelper) : IShopService
{
    public async Task<IEnumerable<ShopResponse>> GetActiveShopsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var shops = await shopRepository.GetAllActiveAsync(search, cancellationToken);
        return shops.Select(shopHelper.ToResponse);
    }

    public async Task<ShopDetailResponse> GetShopByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{id}' was not found.");
        return shopHelper.ToDetailResponse(shop);
    }

    public async Task<ShopDetailResponse> GetShopBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken)
                   ?? throw new HttpException(404, $"Shop with slug '{slug}' was not found.");
        return shopHelper.ToDetailResponse(shop);
    }

    public async Task<ShopResponse?> GetShopBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetBySupplierIdAsync(supplierId, cancellationToken);
        return shop is null ? null : shopHelper.ToResponse(shop);
    }

    public async Task<ShopResponse> CreateShopAsync(CreateShopRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
                       ?? throw new HttpException(404, $"Supplier with id '{request.SupplierId}' was not found.");

        var existingSupplierShop = await shopRepository.GetBySupplierIdAsync(request.SupplierId, cancellationToken);
        if (existingSupplierShop is not null)
        {
            throw new HttpException(409, "This supplier already owns a storefront shop.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var existingSlug = await shopRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (existingSlug is not null)
        {
            throw new HttpException(409, $"A shop with slug '{request.Slug}' already exists.");
        }

        if (request.TemplateId.HasValue)
        {
            var template = await shopTemplateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                           ?? throw new HttpException(404, $"Template with id '{request.TemplateId.Value}' was not found.");
        }

        var shop = new Shop
        {
            SupplierId = request.SupplierId,
            TemplateId = request.TemplateId,
            TemplateOverrides = string.IsNullOrWhiteSpace(request.TemplateOverrides) ? "{}" : request.TemplateOverrides,
            Name = request.Name,
            Slug = normalizedSlug,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            BannerUrl = request.BannerUrl,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await shopRepository.CreateAsync(shop, cancellationToken);
        var fullShop = await shopRepository.GetByIdAsync(created.Id, cancellationToken);
        return shopHelper.ToResponse(fullShop ?? created);
    }

    public async Task<ShopResponse> UpdateShopAsync(long id, UpdateShopRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{id}' was not found.");

        shop.Name = request.Name;
        shop.Description = request.Description;
        shop.LogoUrl = request.LogoUrl;
        shop.BannerUrl = request.BannerUrl;
        shop.IsActive = request.IsActive;
        shop.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await shopRepository.UpdateAsync(shop, cancellationToken);
        return shopHelper.ToResponse(updated);
    }

    public async Task<ShopResponse> ApplyTemplateAsync(long id, ApplyShopTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{id}' was not found.");

        if (request.TemplateId.HasValue)
        {
            var template = await shopTemplateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                           ?? throw new HttpException(404, $"Template with id '{request.TemplateId.Value}' was not found.");
        }

        shop.TemplateId = request.TemplateId;
        shop.TemplateOverrides = string.IsNullOrWhiteSpace(request.TemplateOverrides) ? "{}" : request.TemplateOverrides;
        shop.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await shopRepository.UpdateAsync(shop, cancellationToken);
        var fullShop = await shopRepository.GetByIdAsync(updated.Id, cancellationToken);
        return shopHelper.ToResponse(fullShop ?? updated);
    }

    public async Task DeleteShopAsync(long id, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{id}' was not found.");

        await shopRepository.DeleteAsync(shop.Id, cancellationToken);
    }
}
