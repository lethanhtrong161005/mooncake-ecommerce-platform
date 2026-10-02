namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Tiered and volume-based promotion rules endpoints.</summary>
[Route("api/v1/promotions")]
public class PromotionsController(IPromotionRuleService promotionRuleService) : BaseApiController
{
    /// <summary>Lists promotion rules configured for a shop.</summary>
    [HttpGet("shop/{shopId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByShopIdAsync(long shopId, [FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var rules = await promotionRuleService.GetByShopIdAsync(shopId, activeOnly, cancellationToken);
        return Success(rules, "Shop promotion rules retrieved successfully.");
    }

    /// <summary>Gets a promotion rule by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var rule = await promotionRuleService.GetByIdAsync(id, cancellationToken);
        return Success(rule, "Promotion rule retrieved successfully.");
    }

    /// <summary>Creates a new volume or tiered promotion rule for a shop.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePromotionRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await promotionRuleService.CreateAsync(request, cancellationToken);
        return Created(rule, "Promotion rule created successfully.");
    }

    /// <summary>Updates an existing promotion rule.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdatePromotionRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await promotionRuleService.UpdateAsync(id, request, cancellationToken);
        return Success(rule, "Promotion rule updated successfully.");
    }

    /// <summary>Deletes a promotion rule.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await promotionRuleService.DeleteAsync(id, cancellationToken);
        return Success("Promotion rule deleted successfully.");
    }
}
