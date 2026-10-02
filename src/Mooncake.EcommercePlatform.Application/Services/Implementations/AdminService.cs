namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Admin.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements admin management operations.</summary>
public class AdminService(
    ISupplierRepository supplierRepository,
    IShopTemplateRepository shopTemplateRepository,
    ICategoryRepository categoryRepository,
    ISupplierHelper supplierHelper,
    IShopTemplateHelper shopTemplateHelper,
    ICategoryHelper categoryHelper) : IAdminService
{
    public async Task<IEnumerable<SupplierResponse>> GetSuppliersAsync(bool? isVerified = null, CancellationToken cancellationToken = default)
    {
        var suppliers = await supplierRepository.GetAllAsync(isVerified, cancellationToken);
        return suppliers.Select(supplierHelper.ToResponse);
    }

    public async Task<SupplierResponse> VerifySupplierAsync(long supplierId, VerifySupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                       ?? throw new HttpException(404, $"Supplier with id '{supplierId}' was not found.");

        supplier.IsVerified = request.IsVerified;
        if (request.ReputationScoreAdjustment.HasValue)
        {
            supplier.ReputationScore += request.ReputationScoreAdjustment.Value;
        }
        supplier.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await supplierRepository.UpdateAsync(supplier, cancellationToken);
        return supplierHelper.ToResponse(updated);
    }

    public async Task<IEnumerable<ShopTemplateResponse>> GetShopTemplatesAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var templates = await shopTemplateRepository.GetAllAsync(activeOnly, cancellationToken);
        return templates.Select(shopTemplateHelper.ToResponse);
    }

    public async Task<ShopTemplateResponse> GetShopTemplateByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var template = await shopTemplateRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Shop template with id '{id}' was not found.");
        return shopTemplateHelper.ToResponse(template);
    }

    public async Task<ShopTemplateResponse> CreateShopTemplateAsync(CreateShopTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = new ShopTemplate
        {
            Name = request.Name,
            Description = request.Description,
            PreviewUrl = request.PreviewUrl,
            Config = string.IsNullOrWhiteSpace(request.Config) ? "{}" : request.Config,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await shopTemplateRepository.CreateAsync(template, cancellationToken);
        return shopTemplateHelper.ToResponse(created);
    }

    public async Task<ShopTemplateResponse> UpdateShopTemplateAsync(long id, UpdateShopTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await shopTemplateRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Shop template with id '{id}' was not found.");

        template.Name = request.Name;
        template.Description = request.Description;
        template.PreviewUrl = request.PreviewUrl;
        template.Config = string.IsNullOrWhiteSpace(request.Config) ? "{}" : request.Config;
        template.IsActive = request.IsActive;
        template.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await shopTemplateRepository.UpdateAsync(template, cancellationToken);
        return shopTemplateHelper.ToResponse(updated);
    }

    public async Task DeleteShopTemplateAsync(long id, CancellationToken cancellationToken = default)
    {
        var template = await shopTemplateRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Shop template with id '{id}' was not found.");

        await shopTemplateRepository.DeleteAsync(template.Id, cancellationToken);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
        {
            throw new HttpException(409, $"A category with slug '{request.Slug}' already exists.");
        }

        if (request.ParentId.HasValue)
        {
            var parent = await categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken)
                         ?? throw new HttpException(404, $"Parent category with id '{request.ParentId.Value}' was not found.");
        }

        var category = new Category
        {
            ParentId = request.ParentId,
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await categoryRepository.CreateAsync(category, cancellationToken);
        return categoryHelper.ToResponse(created);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(long id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Category with id '{id}' was not found.");

        if (request.ParentId.HasValue && request.ParentId.Value == id)
        {
            throw new HttpException(400, "A category cannot be its own parent.");
        }

        var existingSlug = await categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existingSlug is not null && existingSlug.Id != id)
        {
            throw new HttpException(409, $"A category with slug '{request.Slug}' already exists.");
        }

        category.ParentId = request.ParentId;
        category.Name = request.Name;
        category.Slug = request.Slug.ToLowerInvariant().Trim();
        category.Description = request.Description;

        var updated = await categoryRepository.UpdateAsync(category, cancellationToken);
        return categoryHelper.ToResponse(updated);
    }

    public async Task DeleteCategoryAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Category with id '{id}' was not found.");

        await categoryRepository.DeleteAsync(category.Id, cancellationToken);
    }
}
