namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

[ShopApi, Authorize(Roles = "Admin"), Route("api/v1/admin/shops"), EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminShopsController(IShopService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminShopQuery query, CancellationToken ct) => Success(await service.ListAdminAsync(Actor, query, ct), C.Success);
    [HttpGet("{shopId:guid}")]
    public async Task<IActionResult> Detail(Guid shopId, CancellationToken ct) => Success(await service.DetailAdminAsync(Actor, shopId, ct), C.Success);
    [HttpPatch("{shopId:guid}/status")]
    public async Task<IActionResult> Moderate(Guid shopId, [FromBody] AdminShopModerationRequest request, CancellationToken ct) =>
        Success(await service.ModerateAsync(Actor, shopId, Request.Headers["Idempotency-Key"].ToString(), request, ct), C.Success);
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
