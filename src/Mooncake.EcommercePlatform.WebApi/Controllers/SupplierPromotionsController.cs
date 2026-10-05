namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Promotion management endpoints for verified suppliers.</summary>
[Authorize(Roles = "Supplier")]
[Route("api/v1/supplier/promotions")]
public sealed class SupplierPromotionsController(IPromotionService promotionService) : BaseApiController
{
    /// <summary>Lists active and inactive promotions owned by the authenticated supplier.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await promotionService.GetMineAsync(GetCurrentUserId(), cancellationToken), "Promotions retrieved successfully.");

    /// <summary>Creates a quantity-tier promotion for the supplier's shop.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] SavePromotionRequest request, CancellationToken cancellationToken) =>
        Created(await promotionService.CreateAsync(GetCurrentUserId(), request, cancellationToken), "Promotion created successfully.");

    /// <summary>Updates a promotion owned by the authenticated supplier.</summary>
    [HttpPut("{promotionId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid promotionId, [FromBody] SavePromotionRequest request, CancellationToken cancellationToken) =>
        Success(await promotionService.UpdateAsync(GetCurrentUserId(), promotionId, request, cancellationToken), "Promotion updated successfully.");

    /// <summary>Deactivates a promotion without deleting its history.</summary>
    [HttpDelete("{promotionId:guid}")]
    public async Task<IActionResult> DeactivateAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        await promotionService.DeactivateAsync(GetCurrentUserId(), promotionId, cancellationToken);
        return Success("Promotion deactivated successfully.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
