namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Storefront and shop management endpoints.</summary>
[Route("api/v1/shops")]
public class ShopsController(IShopService shopService) : BaseApiController
{
    /// <summary>Lists active shops with optional search filter.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var shops = await shopService.GetActiveShopsAsync(search, cancellationToken);
        return Success(shops, "Active shops retrieved successfully.");
    }

    /// <summary>Gets a shop storefront by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var shop = await shopService.GetShopByIdAsync(id, cancellationToken);
        return Success(shop, "Shop details retrieved successfully.");
    }

    /// <summary>Gets a shop storefront by URL slug.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var shop = await shopService.GetShopBySlugAsync(slug, cancellationToken);
        return Success(shop, "Shop details retrieved successfully.");
    }

    /// <summary>Gets shop owned by a supplier ID.</summary>
    [HttpGet("supplier/{supplierId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        var shop = await shopService.GetShopBySupplierIdAsync(supplierId, cancellationToken);
        return Success(shop, "Supplier shop retrieved successfully.");
    }

    /// <summary>Creates a new storefront shop for a supplier.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateShopRequest request, CancellationToken cancellationToken)
    {
        var shop = await shopService.CreateShopAsync(request, cancellationToken);
        return Created(shop, "Shop created successfully.");
    }

    /// <summary>Updates shop storefront details.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateShopRequest request, CancellationToken cancellationToken)
    {
        var shop = await shopService.UpdateShopAsync(id, request, cancellationToken);
        return Success(shop, "Shop updated successfully.");
    }

    /// <summary>Applies a template and template customization overrides to a shop.</summary>
    [HttpPut("{id:long}/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyTemplateAsync(long id, [FromBody] ApplyShopTemplateRequest request, CancellationToken cancellationToken)
    {
        var shop = await shopService.ApplyTemplateAsync(id, request, cancellationToken);
        return Success(shop, "Shop template applied successfully.");
    }

    /// <summary>Deletes/removes a shop.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await shopService.DeleteShopAsync(id, cancellationToken);
        return Success("Shop deleted successfully.");
    }
}
