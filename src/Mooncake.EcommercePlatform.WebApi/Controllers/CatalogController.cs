namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Public catalog discovery and supplier-owned catalog endpoints.</summary>
[Route("api/v1")]
public sealed class CatalogController(ICatalogService catalogService) : BaseApiController
{
    /// <summary>Lists active product categories.</summary>
    [AllowAnonymous]
    [HttpGet("catalog/categories")]
    public async Task<IActionResult> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetCategoriesAsync(cancellationToken), "Categories retrieved successfully.");

    /// <summary>Lists active shop templates.</summary>
    [AllowAnonymous]
    [HttpGet("catalog/shop-templates")]
    public async Task<IActionResult> GetTemplatesAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetTemplatesAsync(cancellationToken), "Shop templates retrieved successfully.");

    /// <summary>Searches public active products.</summary>
    [AllowAnonymous]
    [HttpGet("catalog/products")]
    public async Task<IActionResult> SearchProductsAsync([FromQuery] string? search, [FromQuery] Guid? categoryId,
        [FromQuery] Guid? shopId, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string sortBy = "newest", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Success(await catalogService.SearchProductsAsync(search, categoryId, shopId, minPrice, maxPrice, sortBy, page, pageSize, cancellationToken), "Products retrieved successfully.");

    [AllowAnonymous]
    [HttpGet("catalog/products/suggestions")]
    public async Task<IActionResult> GetProductSuggestionsAsync([FromQuery] string query, CancellationToken cancellationToken) =>
        Success(await catalogService.GetProductSuggestionsAsync(query, cancellationToken), "Product suggestions retrieved successfully.");

    /// <summary>Returns a public product and its variants.</summary>
    [AllowAnonymous]
    [HttpGet("catalog/products/{productId:guid}")]
    public async Task<IActionResult> GetProductAsync(Guid productId, CancellationToken cancellationToken) =>
        Success(await catalogService.GetProductAsync(productId, cancellationToken), "Product retrieved successfully.");

    /// <summary>Returns a public shop by slug.</summary>
    [AllowAnonymous]
    [HttpGet("shops/{slug}")]
    public async Task<IActionResult> GetShopAsync(string slug, CancellationToken cancellationToken) =>
        Success(await catalogService.GetShopAsync(slug, cancellationToken), "Shop retrieved successfully.");

    /// <summary>Creates or updates the authenticated supplier's shop.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpPut("shops/me")]
    public async Task<IActionResult> SaveMyShopAsync([FromBody] SaveShopRequest request, CancellationToken cancellationToken) =>
        Success(await catalogService.SaveMyShopAsync(GetCurrentUserId(), request, cancellationToken), "Shop saved successfully.");

    /// <summary>Creates a product in the authenticated supplier's shop.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpGet("supplier/products")]
    public async Task<IActionResult> GetMyProductsAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetMyProductsAsync(GetCurrentUserId(), cancellationToken), "Products retrieved successfully.");

    [Authorize(Roles = "Supplier")]
    [HttpPost("supplier/products")]
    public async Task<IActionResult> CreateProductAsync([FromBody] SaveProductRequest request, CancellationToken cancellationToken) =>
        Created(await catalogService.CreateProductAsync(GetCurrentUserId(), request, cancellationToken), "Product created successfully.");

    /// <summary>Updates a product owned by the authenticated supplier.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpPut("supplier/products/{productId:guid}")]
    public async Task<IActionResult> UpdateProductAsync(Guid productId, [FromBody] SaveProductRequest request, CancellationToken cancellationToken) =>
        Success(await catalogService.UpdateProductAsync(GetCurrentUserId(), productId, request, cancellationToken), "Product updated successfully.");

    [Authorize(Roles = "Supplier")]
    [HttpPost("supplier/products/{productId:guid}/variants/{variantId:guid}/stock-adjustments")]
    public async Task<IActionResult> AdjustVariantStockAsync(Guid productId, Guid variantId, [FromBody] AdjustStockRequest request, CancellationToken cancellationToken) =>
        Success(await catalogService.AdjustVariantStockAsync(GetCurrentUserId(), productId, variantId, request.QuantityDelta, request.Reason, cancellationToken), "Variant stock adjusted successfully.");

    /// <summary>Soft-deletes a product owned by the authenticated supplier.</summary>
    [Authorize(Roles = "Supplier")]
    [HttpDelete("supplier/products/{productId:guid}")]
    public async Task<IActionResult> DeleteProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await catalogService.DeleteProductAsync(GetCurrentUserId(), productId, cancellationToken);
        return Success("Product removed successfully.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}
