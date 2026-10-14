namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/rfqs")]
public sealed class RfqsController(IRfqService rfqService) : BaseApiController
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRfqRequest request, CancellationToken cancellationToken) =>
        Created(await rfqService.CreateAsync(CurrentUserId, request, cancellationToken), "RFQ created successfully.");

    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await rfqService.GetMineAsync(CurrentUserId, cancellationToken), "RFQs retrieved successfully.");

    [Authorize(Roles = "Customer, Supplier")]
    [HttpGet("{rfqId:guid}")]
    public async Task<IActionResult> GetAsync(Guid rfqId, CancellationToken cancellationToken) =>
        Success(await rfqService.GetAsync(CurrentUserId, User.IsInRole("Supplier"), rfqId, cancellationToken), "RFQ retrieved successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("{rfqId:guid}/publish")]
    public async Task<IActionResult> PublishAsync(Guid rfqId, CancellationToken cancellationToken) =>
        Success(await rfqService.PublishAsync(CurrentUserId, rfqId, cancellationToken), "RFQ published successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("{rfqId:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid rfqId, [FromBody] CancelRfqRequest request, CancellationToken cancellationToken)
    {
        await rfqService.CancelAsync(CurrentUserId, rfqId, request.Reason, cancellationToken);
        return Success("RFQ cancelled successfully.");
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{rfqId:guid}/invitations")]
    public async Task<IActionResult> InviteAsync(Guid rfqId, [FromBody] InviteSuppliersRequest request, CancellationToken cancellationToken) =>
        Success(await rfqService.InviteAsync(CurrentUserId, rfqId, request, cancellationToken), "Suppliers invited successfully.");

    [Authorize(Roles = "Supplier")]
    [HttpPost("{rfqId:guid}/decline")]
    public async Task<IActionResult> DeclineAsync(Guid rfqId, [FromBody] DeclineInvitationRequest request, CancellationToken cancellationToken)
    {
        await rfqService.DeclineAsync(CurrentUserId, rfqId, request, cancellationToken);
        return Success("RFQ invitation declined successfully.");
    }

    [Authorize(Roles = "Supplier")]
    [HttpPost("{rfqId:guid}/bids")]
    public async Task<IActionResult> SubmitBidAsync(Guid rfqId, [FromBody] SubmitBidRequest request, CancellationToken cancellationToken) =>
        Created(await rfqService.SubmitBidAsync(CurrentUserId, rfqId, request, cancellationToken), "Bid submitted successfully.");

    [Authorize(Roles = "Customer")]
    [HttpGet("{rfqId:guid}/bids")]
    public async Task<IActionResult> CompareBidsAsync(Guid rfqId, CancellationToken cancellationToken) =>
        Success(await rfqService.CompareBidsAsync(CurrentUserId, rfqId, cancellationToken), "Bids retrieved successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("{rfqId:guid}/bids/{bidId:guid}/cancel")]
    public async Task<IActionResult> CancelBidAsync(Guid rfqId, Guid bidId, CancellationToken cancellationToken) =>
        Success(await rfqService.CancelBidAsync(CurrentUserId, rfqId, bidId, cancellationToken), "Bid cancelled successfully.");

    [Authorize(Roles = "Customer")]
    [HttpPost("{rfqId:guid}/bids/{bidId:guid}/accept")]
    public async Task<IActionResult> AwardBidAsync(Guid rfqId, Guid bidId, [FromBody] AwardBidRequest request,
        CancellationToken cancellationToken) =>
        Success(await rfqService.AwardBidAsync(CurrentUserId, rfqId, bidId, request, cancellationToken), "Bid accepted and draft contract created.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}

public sealed record CancelRfqRequest(string Reason);
