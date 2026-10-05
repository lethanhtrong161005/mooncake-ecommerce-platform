namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Supplier application and admin verification endpoints.</summary>
[Route("api/v1/suppliers")]
public sealed class SuppliersController(ISupplierService supplierService) : BaseApiController
{
    /// <summary>Submits or resubmits the current user's supplier application.</summary>
    [Authorize]
    [HttpPost("me/apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyAsync([FromBody] ApplySupplierRequest request, CancellationToken cancellationToken)
    {
        var profile = await supplierService.ApplyAsync(GetCurrentUserId(), request, cancellationToken);
        return Success(profile, "Supplier application submitted successfully.");
    }

    /// <summary>Returns the current user's supplier profile and submitted documents.</summary>
    [Authorize(Roles = "Supplier,Admin")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await supplierService.GetMyProfileAsync(GetCurrentUserId(), cancellationToken);
        return Success(profile, "Supplier profile retrieved successfully.");
    }

    /// <summary>Lists supplier applications awaiting review.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingAsync(CancellationToken cancellationToken)
    {
        var profiles = await supplierService.GetPendingAsync(cancellationToken);
        return Success(profiles, "Pending supplier applications retrieved successfully.");
    }

    /// <summary>Approves or rejects a pending supplier application.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{profileId:guid}/review")]
    public async Task<IActionResult> ReviewAsync(Guid profileId, [FromBody] ReviewSupplierRequest request, CancellationToken cancellationToken)
    {
        var profile = await supplierService.ReviewAsync(profileId, GetCurrentUserId(), request, cancellationToken);
        return Success(profile, "Supplier application reviewed successfully.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
