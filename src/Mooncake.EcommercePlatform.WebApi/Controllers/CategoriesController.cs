namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Public catalog categories endpoints.</summary>
[Route("api/v1/categories")]
public class CategoriesController(ICategoryService categoryService) : BaseApiController
{
    /// <summary>Returns the complete hierarchical category tree.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTreeAsync(CancellationToken cancellationToken)
    {
        var tree = await categoryService.GetCategoryTreeAsync(cancellationToken);
        return Success(tree, "Category tree retrieved successfully.");
    }

    /// <summary>Gets category by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetCategoryByIdAsync(id, cancellationToken);
        return Success(category, "Category retrieved successfully.");
    }

    /// <summary>Gets category by slug.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetCategoryBySlugAsync(slug, cancellationToken);
        return Success(category, "Category retrieved successfully.");
    }
}
