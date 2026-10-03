namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for Category mapping helpers.</summary>
public interface ICategoryHelper
{
    CategoryResponse ToResponse(Category category);
    IEnumerable<CategoryResponse> ToTree(IEnumerable<Category> categories);
}
