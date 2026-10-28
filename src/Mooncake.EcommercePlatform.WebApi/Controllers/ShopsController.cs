namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

[ShopApi, Authorize, Route("api/v1/shops"), EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ShopsController(IShopService service) : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken ct) => Success(await service.GetMineAsync(Actor, ct), C.Success);
    [HttpPost("me")]
    public async Task<IActionResult> Create([FromBody] CreateShopRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(Actor, Key, request, ct);
        return Created(result, C.Success);
    }
    [HttpPut("me")]
    public async Task<IActionResult> Update([FromBody] UpdateShopRequest request, CancellationToken ct) => Success(await service.UpdateAsync(Actor, Key, request, ct), C.Success);
    [HttpPatch("me/visibility")]
    public async Task<IActionResult> Visibility([FromBody] SetMyShopVisibilityRequest request, CancellationToken ct) => Success(await service.VisibilityAsync(Actor, Key, request, ct), C.Success);
    [HttpGet("me/appearance")]
    public async Task<IActionResult> Appearance(CancellationToken ct) => Success((await service.GetMineAsync(Actor, ct)).Appearance, C.Success);
    [HttpPut("me/appearance")]
    public async Task<IActionResult> SaveAppearance([FromBody] UpdateShopAppearanceRequest request, CancellationToken ct) => Success(await service.AppearanceAsync(Actor, Key, request, ct), C.Success);
    [HttpGet("me/preview")]
    public async Task<IActionResult> Preview(CancellationToken ct) => Success(await service.PreviewAsync(Actor, ct), C.Success);
    [HttpPost("me/media"), RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Media([FromForm] ShopMediaUploadRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();
        return Created(await service.UploadAsync(Actor, false, Key, request.Purpose, stream, ct), C.Success);
    }
    [AllowAnonymous, HttpGet("{slug}")]
    public async Task<IActionResult> Public(string slug, CancellationToken ct) => Success(await service.PublicAsync(slug, ct), C.Success);
    [AllowAnonymous, HttpGet("{slug}/products")]
    public async Task<IActionResult> Products(string slug, [FromQuery] ShopProductQuery query, CancellationToken ct) => Success(await service.ProductsAsync(slug, query, ct), C.Success);
    [AllowAnonymous, HttpGet("{slug}/categories")]
    public async Task<IActionResult> Categories(string slug, [FromQuery] ShopPageQuery query, CancellationToken ct) => Success(await service.CategoriesAsync(slug, query, ct), C.Success);
    [AllowAnonymous, HttpGet("{slug}/reviews")]
    public async Task<IActionResult> Reviews(string slug, [FromQuery] ShopReviewQuery query, CancellationToken ct) => Success(await service.ReviewsAsync(slug, query, ct), C.Success);
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Key => Request.Headers["Idempotency-Key"].ToString();
}

public sealed record ShopMediaUploadRequest
{
    [Required] public IFormFile File { get; init; } = null!;
    [Required, RegularExpression("^(Logo|Banner|TemplatePreview)$")] public string Purpose { get; init; } = string.Empty;
}
