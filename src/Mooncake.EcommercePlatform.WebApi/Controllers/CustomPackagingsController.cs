namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Customer custom gift packaging and logo printing endpoints.</summary>
[Route("api/v1/custom-packagings")]
public class CustomPackagingsController(ICustomPackagingService packagingService) : BaseApiController
{
    /// <summary>Lists custom packaging designs for a customer.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomerAsync([FromQuery] long customerId, CancellationToken cancellationToken)
    {
        var packagings = await packagingService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Success(packagings, "Custom packagings retrieved successfully.");
    }

    /// <summary>Gets a custom packaging design by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var packaging = await packagingService.GetByIdAsync(id, cancellationToken);
        return Success(packaging, "Custom packaging retrieved successfully.");
    }

    /// <summary>Creates a new custom packaging design with logo.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCustomPackagingRequest request, CancellationToken cancellationToken)
    {
        var packaging = await packagingService.CreateAsync(request, cancellationToken);
        return Created(packaging, "Custom packaging created successfully.");
    }

    /// <summary>Updates an existing custom packaging design.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] UpdateCustomPackagingRequest request, CancellationToken cancellationToken)
    {
        var packaging = await packagingService.UpdateAsync(id, request, cancellationToken);
        return Success(packaging, "Custom packaging updated successfully.");
    }

    /// <summary>Deletes a custom packaging design.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await packagingService.DeleteAsync(id, cancellationToken);
        return Success("Custom packaging deleted successfully.");
    }
}
