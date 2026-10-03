namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Direct variant mutation and stock inventory management endpoints.</summary>
[Route("api/v1/variants")]
public class ProductVariantsController(IProductService productService) : BaseApiController
{
    /// <summary>Gets a variant by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var variant = await productService.GetVariantByIdAsync(id, cancellationToken);
        return Success(variant, "Variant details retrieved successfully.");
    }

    /// <summary>Updates variant pricing, min order quantity, or SKU.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateProductVariantRequest request, CancellationToken cancellationToken)
    {
        var variant = await productService.UpdateVariantAsync(id, request, cancellationToken);
        return Success(variant, "Variant updated successfully.");
    }

    /// <summary>Updates stock inventory quantity for a variant.</summary>
    [HttpPut("{id:long}/stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStockAsync(long id, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken)
    {
        var variant = await productService.UpdateStockAsync(id, request, cancellationToken);
        return Success(variant, "Stock inventory updated successfully.");
    }

    /// <summary>Deletes a product variant.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await productService.DeleteVariantAsync(id, cancellationToken);
        return Success("Variant deleted successfully.");
    }
}
