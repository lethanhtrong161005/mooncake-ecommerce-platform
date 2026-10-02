namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Catalog products, search, gallery, and variant management endpoints.</summary>
[Route("api/v1/products")]
public class ProductsController(IProductService productService) : BaseApiController
{
    /// <summary>Searches and filters products with pagination.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync([FromQuery] ProductQueryParameters parameters, CancellationToken cancellationToken)
    {
        var (items, totalCount, page, pageSize) = await productService.SearchProductsAsync(parameters, cancellationToken);
        var result = new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
        return Success(result, "Products retrieved successfully.");
    }

    /// <summary>Gets product detail by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
        return Success(product, "Product details retrieved successfully.");
    }

    /// <summary>Creates a new product in a shop.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateProductAsync(request, cancellationToken);
        return Created(product, "Product created successfully.");
    }

    /// <summary>Updates product details.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateProductAsync(id, request, cancellationToken);
        return Success(product, "Product updated successfully.");
    }

    /// <summary>Deletes a product.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await productService.DeleteProductAsync(id, cancellationToken);
        return Success("Product deleted successfully.");
    }

    // ── Product Variants sub-routes ──────────────────────────────────────
    /// <summary>Lists all variants for a product.</summary>
    [HttpGet("{productId:long}/variants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariantsAsync(long productId, CancellationToken cancellationToken)
    {
        var variants = await productService.GetVariantsAsync(productId, cancellationToken);
        return Success(variants, "Product variants retrieved successfully.");
    }

    /// <summary>Creates a new variant for a product.</summary>
    [HttpPost("{productId:long}/variants")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVariantAsync(long productId, [FromBody] CreateProductVariantRequest request, CancellationToken cancellationToken)
    {
        var variant = await productService.CreateVariantAsync(productId, request, cancellationToken);
        return Created(variant, "Product variant created successfully.");
    }

    // ── Product Images sub-routes ────────────────────────────────────────
    /// <summary>Attaches an image to a product gallery.</summary>
    [HttpPost("{productId:long}/images")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImageAsync(long productId, [FromBody] CreateProductImageRequest request, CancellationToken cancellationToken)
    {
        var image = await productService.AddImageAsync(productId, request, cancellationToken);
        return Created(image, "Product image attached successfully.");
    }

    /// <summary>Removes an image from a product.</summary>
    [HttpDelete("{productId:long}/images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteImageAsync(long productId, long imageId, CancellationToken cancellationToken)
    {
        await productService.DeleteImageAsync(imageId, cancellationToken);
        return Success("Product image deleted successfully.");
    }

    /// <summary>Sets an image as the primary thumbnail for a product.</summary>
    [HttpPut("{productId:long}/images/{imageId:long}/primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImageAsync(long productId, long imageId, CancellationToken cancellationToken)
    {
        await productService.SetPrimaryImageAsync(productId, imageId, cancellationToken);
        return Success("Primary image set successfully.");
    }
}
