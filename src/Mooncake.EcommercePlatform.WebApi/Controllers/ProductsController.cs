namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.ProductConstants;

[ProductApi, AllowAnonymous, Route("api/v1/products"), EnableRateLimiting(C.RatePolicy)]
public sealed class ProductsController(IProductCatalogService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ProductSearchQuery query, CancellationToken ct) =>
        Success(await service.SearchAsync(query, ct), C.Success);

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] ProductAutocompleteQuery query, CancellationToken ct) =>
        Success(await service.AutocompleteAsync(query, ct), C.Success);

    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations([FromQuery] int limit = 20, CancellationToken ct = default) =>
        Success(await service.RecommendationsAsync(limit, UserId, ct), C.Success);

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Detail(Guid productId, CancellationToken ct) =>
        Success(await service.PublicAsync(productId, ct), C.Success);

    [HttpGet("{productId:guid}/related")]
    public async Task<IActionResult> Related(Guid productId, [FromQuery] int limit = 12, CancellationToken ct = default) =>
        Success(await service.RelatedAsync(productId, limit, UserId, ct), C.Success);

    [HttpPost("{productId:guid}/views")]
    public async Task<IActionResult> RecordView(Guid productId, [FromBody] RecordProductViewRequest request, CancellationToken ct)
    {
        await service.RecordViewAsync(productId, UserId, request, ct);
        return AcceptedResponse(C.Success);
    }

    private Guid? UserId => User.Identity?.IsAuthenticated == true &&
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
