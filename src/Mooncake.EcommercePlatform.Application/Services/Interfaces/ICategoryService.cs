namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;

/// <summary>Contract for browsing public category trees.</summary>
public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
