namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ProductConstants;

[ProductApi, Authorize, Route("api/v1/suppliers/me/products"), EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SupplierProductsController(IProductCatalogService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OwnerProductQuery query, CancellationToken ct) =>
        Success(await service.ListMineAsync(Actor, query, ct), C.Success);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct) =>
        Created(await service.CreateAsync(Actor, Key, request, ct), C.Success);

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Detail(Guid productId, CancellationToken ct) =>
        Success(await service.GetMineAsync(Actor, productId, ct), C.Success);

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductCoreRequest request, CancellationToken ct) =>
        Success(await service.UpdateAsync(Actor, productId, Key, request, ct), C.Success);

    [HttpPost("{productId:guid}/variants")]
    public async Task<IActionResult> AddVariant(Guid productId, [FromBody] CreateProductVariantRequest request, CancellationToken ct) =>
        Created(await service.AddVariantAsync(Actor, productId, Key, request, ct), C.Success);

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid productId, Guid variantId,
        [FromBody] UpdateProductVariantRequest request, CancellationToken ct) =>
        Success(await service.UpdateVariantAsync(Actor, productId, variantId, Key, request, ct), C.Success);

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId,
        [FromQuery, Range(1, long.MaxValue)] long expectedVersion, CancellationToken ct) =>
        Success(await service.DeleteVariantAsync(Actor, productId, variantId, expectedVersion, Key, ct), C.Success);

    [HttpPost("{productId:guid}/variants/{variantId:guid}/inventory-adjustments")]
    public async Task<IActionResult> AdjustInventory(Guid productId, Guid variantId,
        [FromBody] AdjustVariantInventoryRequest request, CancellationToken ct) =>
        Success(await service.AdjustInventoryAsync(Actor, productId, variantId, Key, request, ct), C.Success);

    [HttpPut("{productId:guid}/gallery")]
    public async Task<IActionResult> Gallery(Guid productId, [FromBody] ReplaceProductGalleryRequest request, CancellationToken ct) =>
        Success(await service.ReplaceGalleryAsync(Actor, productId, Key, request, ct), C.Success);

    [HttpPatch("{productId:guid}/publication")]
    public async Task<IActionResult> Publication(Guid productId, [FromBody] SetProductPublicationRequest request, CancellationToken ct) =>
        Success(await service.SetPublicationAsync(Actor, productId, Key, request, ct), C.Success);

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, [FromQuery, Range(1, long.MaxValue)] long expectedVersion, CancellationToken ct) =>
        Success(await service.DeleteAsync(Actor, productId, expectedVersion, Key, ct), C.Success);

    [HttpPost("~/api/v1/suppliers/me/product-media"), RequestSizeLimit(6 * 1024 * 1024),
     RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Media([FromForm] ProductMediaUploadRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();
        return Created(await service.UploadMediaAsync(Actor, Key, request.Purpose, stream, ct), C.Success);
    }

    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Key => Request.Headers["Idempotency-Key"].ToString();
}

public sealed record ProductMediaUploadRequest
{
    [Required] public IFormFile File { get; init; } = null!;
    [Required, RegularExpression("^(ProductGallery|VariantImage)$")] public string Purpose { get; init; } = string.Empty;
}
