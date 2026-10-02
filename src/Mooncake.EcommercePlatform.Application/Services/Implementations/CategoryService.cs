namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Implements public category retrieval.</summary>
public class CategoryService(ICategoryRepository categoryRepository, ICategoryHelper categoryHelper) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        return categoryHelper.ToTree(categories);
    }

    public async Task<CategoryResponse> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new HttpException(404, $"Category with id '{id}' was not found.");
        return categoryHelper.ToResponse(category);
    }

    public async Task<CategoryResponse> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken)
                       ?? throw new HttpException(404, $"Category with slug '{slug}' was not found.");
        return categoryHelper.ToResponse(category);
    }
}
