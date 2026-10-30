namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Customer and supplier custom packaging collaboration endpoints.</summary>
[Authorize]
[Route("api/v1/orders/{orderId:guid}/custom-packaging")]
public sealed class CustomPackagingController(ICustomPackagingService service) : BaseApiController
{
    /// <summary>Creates the custom packaging brief for a customer order.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(Guid orderId,
        [FromBody] CreateCustomPackagingRequest request, CancellationToken cancellationToken) =>
        Created(await service.CreateAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            B2BOrderingMessages.PackagingCreated);

    /// <summary>Returns a packaging brief and its complete revision timeline.</summary>
    [Authorize(Roles = "Customer,Supplier")]
    [HttpGet]
    public async Task<IActionResult> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await service.GetAsync(GetCurrentUserId(), IsSupplier(), orderId, cancellationToken),
            B2BOrderingMessages.PackagingLoaded);

    /// <summary>Submits a supplier mockup revision for customer review.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("revisions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitRevisionAsync(Guid orderId,
        [FromBody] SubmitPackagingRevisionRequest request, CancellationToken cancellationToken) =>
        Created(await service.SubmitRevisionAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            B2BOrderingMessages.PackagingRevisionSubmitted);

    /// <summary>Approves a supplier mockup or asks for another revision.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("revisions/{revisionId:guid}/review")]
    public async Task<IActionResult> ReviewRevisionAsync(Guid orderId, Guid revisionId,
        [FromBody] ReviewPackagingRevisionRequest request, CancellationToken cancellationToken) =>
        Success(await service.ReviewRevisionAsync(GetCurrentUserId(), orderId, revisionId, request,
            cancellationToken), B2BOrderingMessages.PackagingRevisionReviewed);

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsSupplier() => User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true");
}
