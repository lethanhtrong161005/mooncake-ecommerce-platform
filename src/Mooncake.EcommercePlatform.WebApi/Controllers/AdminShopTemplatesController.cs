namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

[ShopApi, Authorize(Roles = "Admin"), Route("api/v1/admin/shop-templates"), EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminShopTemplatesController(IShopService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminTemplateQuery query, CancellationToken ct) => Success(await service.TemplatesAdminAsync(Actor, query, ct), C.Success);
    [HttpGet("{templateId:guid}")]
    public async Task<IActionResult> Detail(Guid templateId, CancellationToken ct) => Success(await service.TemplateDetailAsync(Actor, templateId, ct), C.Success);
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShopTemplateRequest request, CancellationToken ct) => Created(await service.CreateTemplateAsync(Actor, Key, request, ct), C.Success);
    [HttpPut("{templateId:guid}")]
    public async Task<IActionResult> Publish(Guid templateId, [FromBody] PublishTemplateRevisionRequest request, CancellationToken ct) => Success(await service.PublishTemplateAsync(Actor, templateId, Key, request, ct), C.Success);
    [HttpPatch("{templateId:guid}/availability")]
    public async Task<IActionResult> Availability(Guid templateId, [FromBody] SetTemplateAvailabilityRequest request, CancellationToken ct) => Success(await service.TemplateAvailabilityAsync(Actor, templateId, Key, request, ct), C.Success);
    [HttpPatch("{templateId:guid}/premium")]
    public async Task<IActionResult> Premium(Guid templateId, [FromBody] SetTemplatePremiumRequest request, CancellationToken ct) => Success(await service.TemplatePremiumAsync(Actor, templateId, Key, request, ct), C.Success);
    [HttpPost("media"), RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Media([FromForm] ShopMediaUploadRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();
        return Created(await service.UploadAsync(Actor, true, Key, request.Purpose, stream, ct), C.Success);
    }
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Key => Request.Headers["Idempotency-Key"].ToString();
}
