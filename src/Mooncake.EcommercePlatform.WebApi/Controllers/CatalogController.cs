namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/products")]
public sealed class CatalogController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] string? search, [FromQuery] Guid? categoryId,
        [FromQuery] Guid? shopId, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] int? minOrderQty, [FromQuery] bool? supportsCustomPackaging,
        [FromQuery] string? sortBy, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Success(await catalogService.SearchAsync(search, categoryId, shopId, minPrice, maxPrice, minOrderQty,
            supportsCustomPackaging, sortBy, page, pageSize, cancellationToken), "Products retrieved successfully.");

    [HttpGet("featured")]
    public async Task<IActionResult> FeaturedAsync([FromQuery] int limit = 12,
        CancellationToken cancellationToken = default) =>
        Success(await catalogService.GetFeaturedAsync(limit, cancellationToken), "Featured products retrieved successfully.");

    [HttpGet("suggestions")]
    public async Task<IActionResult> SuggestionsAsync([FromQuery] string query, [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default) =>
        Success(await catalogService.GetSuggestionsAsync(query, limit, cancellationToken), "Product suggestions retrieved successfully.");

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> DetailsAsync(Guid productId, CancellationToken cancellationToken) =>
        Success(await catalogService.GetProductAsync(productId, cancellationToken), "Product retrieved successfully.");
}

[Authorize(Roles = "Supplier")]
[Route("api/v1/supplier/products")]
public sealed class SupplierCatalogController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetSupplierProductsAsync(CurrentUserId, cancellationToken), "Products retrieved successfully.");

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] SaveProductRequest request,
        CancellationToken cancellationToken) =>
        Created(await catalogService.SaveSupplierProductAsync(CurrentUserId, null, request, cancellationToken),
            "Product created successfully.");

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid productId, [FromBody] SaveProductRequest request,
        CancellationToken cancellationToken) =>
        Success(await catalogService.SaveSupplierProductAsync(CurrentUserId, productId, request, cancellationToken),
            "Product updated successfully.");

    [HttpPost("{productId:guid}/stock-adjustments")]
    public async Task<IActionResult> AdjustStockAsync(Guid productId, [FromBody] AdjustProductStockRequest request,
        CancellationToken cancellationToken) =>
        Success(await catalogService.AdjustStockAsync(CurrentUserId, productId, request, cancellationToken),
            "Product stock adjusted successfully.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}

[Route("api/v1/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetCategoriesAsync(cancellationToken), "Categories retrieved successfully.");
}

[Route("api/v1/shops")]
public sealed class ShopsController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet("templates")]
    public async Task<IActionResult> TemplatesAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetActiveShopTemplatesAsync(cancellationToken), "Shop templates retrieved successfully.");

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetAsync(string slug, CancellationToken cancellationToken) =>
        Success(await catalogService.GetShopAsync(slug, cancellationToken), "Shop retrieved successfully.");
}

[Authorize(Roles = "Supplier")]
[Route("api/v1/suppliers/me/shop")]
public sealed class SupplierShopController(ICatalogService catalogService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await catalogService.GetSupplierShopAsync(CurrentUserId, cancellationToken), "Supplier shop retrieved successfully.");

    [HttpPut]
    public async Task<IActionResult> SaveAsync([FromBody] SaveShopRequest request,
        CancellationToken cancellationToken) =>
        Success(await catalogService.SaveSupplierShopAsync(CurrentUserId, request, cancellationToken), "Supplier shop saved successfully.");

    [HttpPatch("status")]
    public async Task<IActionResult> SetStatusAsync([FromBody] SetSupplierShopStatusRequest request,
        CancellationToken cancellationToken) =>
        Success(await catalogService.SetSupplierShopStatusAsync(CurrentUserId, request.Status, cancellationToken),
            "Supplier shop status updated successfully.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}

public sealed record SetSupplierShopStatusRequest(Mooncake.EcommercePlatform.Domain.Enums.ShopStatus Status);
