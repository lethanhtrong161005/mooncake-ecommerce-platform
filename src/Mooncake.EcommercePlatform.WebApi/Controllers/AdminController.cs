namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Admin.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Categories.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.ShopTemplates.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Admin operations: supplier verification, template and category management.</summary>
[Route("api/v1/admin")]
public class AdminController(IAdminService adminService) : BaseApiController
{
    // ── Suppliers ──────────────────────────────────────────────────────────
    /// <summary>Lists all suppliers with optional verification filter.</summary>
    [HttpGet("suppliers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliersAsync([FromQuery] bool? isVerified, CancellationToken cancellationToken)
    {
        var suppliers = await adminService.GetSuppliersAsync(isVerified, cancellationToken);
        return Success(suppliers, "Suppliers retrieved successfully.");
    }

    /// <summary>Verifies or rejects a supplier account.</summary>
    [HttpPut("suppliers/{id:long}/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifySupplierAsync(long id, [FromBody] VerifySupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await adminService.VerifySupplierAsync(id, request, cancellationToken);
        return Success(supplier, "Supplier verification status updated successfully.");
    }

    // ── Shop Templates ───────────────────────────────────────────────────
    /// <summary>Lists all platform shop templates.</summary>
    [HttpGet("shop-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShopTemplatesAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var templates = await adminService.GetShopTemplatesAsync(activeOnly, cancellationToken);
        return Success(templates, "Shop templates retrieved successfully.");
    }

    /// <summary>Gets a shop template by ID.</summary>
    [HttpGet("shop-templates/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopTemplateByIdAsync(long id, CancellationToken cancellationToken)
    {
        var template = await adminService.GetShopTemplateByIdAsync(id, cancellationToken);
        return Success(template, "Shop template retrieved successfully.");
    }

    /// <summary>Creates a new shop template.</summary>
    [HttpPost("shop-templates")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShopTemplateAsync([FromBody] CreateShopTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await adminService.CreateShopTemplateAsync(request, cancellationToken);
        return Created(template, "Shop template created successfully.");
    }

    /// <summary>Updates an existing shop template.</summary>
    [HttpPut("shop-templates/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShopTemplateAsync(long id, [FromBody] UpdateShopTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await adminService.UpdateShopTemplateAsync(id, request, cancellationToken);
        return Success(template, "Shop template updated successfully.");
    }

    /// <summary>Deletes a shop template.</summary>
    [HttpDelete("shop-templates/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShopTemplateAsync(long id, CancellationToken cancellationToken)
    {
        await adminService.DeleteShopTemplateAsync(id, cancellationToken);
        return Success("Shop template deleted successfully.");
    }

    // ── Categories ───────────────────────────────────────────────────────
    /// <summary>Creates a new category (Admin only).</summary>
    [HttpPost("categories")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await adminService.CreateCategoryAsync(request, cancellationToken);
        return Created(category, "Category created successfully.");
    }

    /// <summary>Updates an existing category.</summary>
    [HttpPut("categories/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategoryAsync(long id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await adminService.UpdateCategoryAsync(id, request, cancellationToken);
        return Success(category, "Category updated successfully.");
    }

    /// <summary>Deletes a category.</summary>
    [HttpDelete("categories/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategoryAsync(long id, CancellationToken cancellationToken)
    {
        await adminService.DeleteCategoryAsync(id, cancellationToken);
        return Success("Category deleted successfully.");
    }
}
