namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Category entities to DTOs and builds hierarchical category trees.</summary>
public class CategoryHelper : ICategoryHelper
{
    public CategoryResponse ToResponse(Category category) =>
        new(
            category.Id,
            category.ParentId,
            category.Name,
            category.Slug,
            category.Description,
            category.CreatedAtUtc,
            category.Children?.Select(ToResponse).ToList()
        );

    public IEnumerable<CategoryResponse> ToTree(IEnumerable<Category> categories)
    {
        var list = categories.ToList();
        var rootCategories = list.Where(c => c.ParentId == null).ToList();

        return rootCategories.Select(root =>
        {
            var children = list.Where(c => c.ParentId == root.Id).Select(ToResponse).ToList();
            return new CategoryResponse(
                root.Id,
                root.ParentId,
                root.Name,
                root.Slug,
                root.Description,
                root.CreatedAtUtc,
                children.Count != 0 ? children : null
            );
        });
    }
}
