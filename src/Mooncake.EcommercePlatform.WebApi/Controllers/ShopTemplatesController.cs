namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ShopConstants;

[ShopApi, Authorize, Route("api/v1/shop-templates"), EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ShopTemplatesController(IShopService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Catalog([FromQuery] ShopPageQuery query, CancellationToken ct) =>
        Success(await service.CatalogAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), query, ct), C.Success);
}
