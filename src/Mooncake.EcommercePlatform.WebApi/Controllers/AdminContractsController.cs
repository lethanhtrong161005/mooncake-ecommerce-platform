namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Authorize(Roles = "Admin")]
[Route("api/v1/admin/contracts")]
public sealed class AdminContractsController(IContractService contractService) : BaseApiController
{
    [HttpPost("{contractId:guid}/resolve-dispute")]
    public async Task<IActionResult> ResolveDisputeAsync(Guid contractId,
        [FromBody] AdminResolveDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.ResolveDisputeAsync(CurrentUserId, contractId, request, cancellationToken);
        return Success(result, "Dispute resolved successfully.");
    }

    [HttpGet("/api/v1/admin/disputes")]
    public async Task<IActionResult> ListDisputesAsync([FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        var result = await contractService.ListDisputesAsync(status, cancellationToken);
        return Success(result, "Lấy danh sách tranh chấp hợp đồng thành công.");
    }

    [HttpPost("/api/v1/admin/disputes/{disputeId:guid}/resolve")]
    public async Task<IActionResult> ResolveDisputeByIdAsync(Guid disputeId,
        [FromBody] AdminResolveDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.ResolveDisputeByIdAsync(CurrentUserId, disputeId, request, cancellationToken);
        return Success(result, "Phân xử tranh chấp hợp đồng thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}

