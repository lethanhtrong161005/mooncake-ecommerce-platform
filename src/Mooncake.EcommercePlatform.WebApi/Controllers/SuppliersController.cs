namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Supplier application and admin verification endpoints.</summary>
[Route("api/v1/suppliers")]
public sealed class SuppliersController(ISupplierService supplierService) : BaseApiController
{
    /// <summary>Submits or resubmits the current user's supplier application.</summary>
    [Authorize]
    [HttpPost("me/apply")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyAsync([FromBody] ApplySupplierRequest request, CancellationToken cancellationToken)
    {
        var profile = await supplierService.ApplyAsync(GetCurrentUserId(), request, cancellationToken);
        return Success(profile, MessageConstant.SupplierApplied);
    }

    /// <summary>Returns the current user's supplier profile and submitted documents.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await supplierService.GetMyProfileAsync(GetCurrentUserId(), cancellationToken);
        return Success(profile, MessageConstant.SupplierProfileLoaded);
    }

    /// <summary>Lists supplier applications awaiting review.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingAsync(CancellationToken cancellationToken)
    {
        var profiles = await supplierService.GetPendingAsync(cancellationToken);
        return Success(profiles, MessageConstant.SupplierPendingLoaded);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("verified")]
    public async Task<IActionResult> GetVerifiedAsync(CancellationToken cancellationToken) =>
        Success(await supplierService.GetVerifiedAsync(cancellationToken), MessageConstant.SupplierVerifiedLoaded);

    [Authorize(Roles = "Admin")]
    [HttpGet("{profileId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        Success(await supplierService.GetByIdAsync(profileId, cancellationToken), MessageConstant.SupplierProfileLoaded);

    /// <summary>Approves or rejects a pending supplier application.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{profileId:guid}/review")]
    public async Task<IActionResult> ReviewAsync(Guid profileId, [FromBody] ReviewSupplierRequest request, CancellationToken cancellationToken)
    {
        var profile = await supplierService.ReviewAsync(profileId, GetCurrentUserId(), request, cancellationToken);
        return Success(profile, request.Approve ? MessageConstant.SupplierApproved : MessageConstant.SupplierRejected);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{profileId:guid}/revoke")]
    public async Task<IActionResult> RevokeAsync(Guid profileId, [FromBody] RevokeSupplierRequest request, CancellationToken cancellationToken)
    {
        var profile = await supplierService.RevokeAsync(profileId, GetCurrentUserId(), request.Reason, cancellationToken);
        return Success(profile, MessageConstant.SupplierRevoked);
    }

    private Guid GetCurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new HttpException(401, MessageConstant.SessionExpired);
}
