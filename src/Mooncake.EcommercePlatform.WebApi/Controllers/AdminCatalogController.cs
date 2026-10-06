namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Admin management endpoints for catalog categories and shop templates.</summary>
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/catalog")]
public sealed class AdminCatalogController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetCategoriesAsync(cancellationToken), "Categories retrieved successfully.");

    [HttpGet("shop-templates")]
    public async Task<IActionResult> GetShopTemplatesAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetAdminTemplatesAsync(cancellationToken), "Shop templates retrieved successfully.");

    /// <summary>Creates a product category.</summary>
    [HttpPost("categories")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        Created(await catalogService.CreateCategoryAsync(GetCurrentUserId(), request, cancellationToken), "Category created successfully.");

    /// <summary>Updates a product category.</summary>
    [HttpPut("categories/{categoryId:guid}")]
    public async Task<IActionResult> UpdateCategoryAsync(Guid categoryId, [FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        Success(await catalogService.UpdateCategoryAsync(GetCurrentUserId(), categoryId, request, cancellationToken), "Category updated successfully.");

    /// <summary>Soft-deletes a category that has no products or child categories.</summary>
    [HttpDelete("categories/{categoryId:guid}")]
    public async Task<IActionResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await catalogService.DeleteCategoryAsync(GetCurrentUserId(), categoryId, cancellationToken);
        return Success("Category removed successfully.");
    }

    /// <summary>Creates a shop template.</summary>
    [HttpPost("shop-templates")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateShopTemplateAsync([FromBody] SaveShopTemplateRequest request, CancellationToken cancellationToken) =>
        Created(await catalogService.CreateShopTemplateAsync(GetCurrentUserId(), request, cancellationToken), "Shop template created successfully.");

    /// <summary>Updates a shop template.</summary>
    [HttpPut("shop-templates/{templateId:guid}")]
    public async Task<IActionResult> UpdateShopTemplateAsync(Guid templateId, [FromBody] SaveShopTemplateRequest request, CancellationToken cancellationToken) =>
        Success(await catalogService.UpdateShopTemplateAsync(GetCurrentUserId(), templateId, request, cancellationToken), "Shop template updated successfully.");

    /// <summary>Soft-deletes a shop template that is not assigned to a shop.</summary>
    [HttpDelete("shop-templates/{templateId:guid}")]
    public async Task<IActionResult> DeleteShopTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        await catalogService.DeleteShopTemplateAsync(GetCurrentUserId(), templateId, cancellationToken);
        return Success("Shop template removed successfully.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
