namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ProductConstants;

[ProductApi, AllowAnonymous, Route("api/v1"), EnableRateLimiting(C.RatePolicy)]
public sealed class CatalogMetadataController(IProductCatalogService service) : BaseApiController
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) =>
        Success(await service.CategoriesAsync(ct), C.Success);

    [HttpGet("regions")]
    public async Task<IActionResult> Regions(CancellationToken ct) =>
        Success(await service.RegionsAsync(ct), C.Success);
}
