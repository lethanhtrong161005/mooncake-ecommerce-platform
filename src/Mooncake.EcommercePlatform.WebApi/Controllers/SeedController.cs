namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Demo database seeder endpoint covering all 30 platform tables.</summary>
[Route("api/v1/seed")]
public class SeedController(ISeedService seedService) : BaseApiController
{
    /// <summary>Populates the database with realistic demo records across all 30 tables for live presentations.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        var message = await seedService.SeedDemoDataAsync(cancellationToken);
        return Success(message, message);
    }
}
