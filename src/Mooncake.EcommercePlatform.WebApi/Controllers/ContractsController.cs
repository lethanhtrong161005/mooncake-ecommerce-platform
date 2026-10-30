namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/contracts")]
[Authorize(Roles = "Customer,Supplier")]
public sealed class ContractsController(IContractService contractService) : BaseApiController
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await contractService.GetMineAsync(CurrentUserId, User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true"), cancellationToken), "Contracts retrieved successfully.");

    [HttpGet("{contractId:guid}")]
    public async Task<IActionResult> GetAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.GetAsync(CurrentUserId, contractId, cancellationToken), "Contract retrieved successfully.");

    [HttpPost("{contractId:guid}/signature-otp")]
    public async Task<IActionResult> RequestSignatureOtpAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.RequestSignatureOtpAsync(CurrentUserId, contractId, cancellationToken), "A signature code was sent to your verified email.");

    [HttpPost("{contractId:guid}/submit-for-signature")]
    public async Task<IActionResult> BeginSignatureAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.BeginSignatureAsync(CurrentUserId, contractId, cancellationToken), "Contract is pending signature.");

    [HttpPost("{contractId:guid}/sign")]
    public async Task<IActionResult> SignAsync(Guid contractId, [FromBody] SignContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contractService.SignAsync(CurrentUserId, contractId, request.OtpCode, request.ConfirmTerms,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return Success(result, "Contract signature recorded successfully.");
    }

    [HttpGet("{contractId:guid}/milestones")]
    public async Task<IActionResult> GetMilestonesAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.GetMilestonesAsync(CurrentUserId, contractId, cancellationToken), "Milestones retrieved successfully.");

    [HttpPost("{contractId:guid}/milestones/{milestoneId:guid}/submit-progress")]
    public async Task<IActionResult> SubmitMilestoneProgressAsync(Guid contractId, Guid milestoneId,
        [FromBody] SubmitMilestoneProgressRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.SubmitMilestoneProgressAsync(CurrentUserId, contractId, milestoneId, request, cancellationToken);
        return Success(result, "Milestone progress evidence submitted successfully.");
    }

    [HttpPost("{contractId:guid}/milestones/{milestoneId:guid}/verify")]
    public async Task<IActionResult> VerifyMilestoneAsync(Guid contractId, Guid milestoneId,
        [FromBody] VerifyMilestoneRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.VerifyMilestoneAsync(CurrentUserId, contractId, milestoneId, request, cancellationToken);
        return Success(result, request.IsApproved ? "Milestone verified and approved successfully." : "Milestone verification rejected.");
    }

    [HttpPost("{contractId:guid}/extension-request")]
    public async Task<IActionResult> RequestExtensionAsync(Guid contractId,
        [FromBody] RequestContractExtensionRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.RequestExtensionAsync(CurrentUserId, contractId, request, cancellationToken);
        return Success(result, "Deadline extension request submitted successfully.");
    }

    [HttpPost("{contractId:guid}/extension-request/respond")]
    public async Task<IActionResult> RespondExtensionAsync(Guid contractId,
        [FromBody] RespondContractExtensionRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.RespondExtensionAsync(CurrentUserId, contractId, request, cancellationToken);
        return Success(result, request.IsApproved ? "Deadline extension approved." : "Deadline extension rejected.");
    }

    [HttpPost("{contractId:guid}/dispute")]
    public async Task<IActionResult> RaiseDisputeAsync(Guid contractId,
        [FromBody] RaiseContractDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.RaiseDisputeAsync(CurrentUserId, contractId, request, cancellationToken);
        return Success(result, "Dispute raised successfully. Escrow funds are temporarily frozen.");
    }

    [HttpPost("{contractId:guid}/request-final-acceptance")]
    public async Task<IActionResult> RequestFinalAcceptanceAsync(Guid contractId, CancellationToken cancellationToken)
    {
        await contractService.RequestFinalAcceptanceAsync(CurrentUserId, contractId, cancellationToken);
        return Success("Final acceptance requested successfully.");
    }

    [HttpPost("{contractId:guid}/confirm-final-acceptance")]
    public async Task<IActionResult> ConfirmFinalAcceptanceAsync(Guid contractId,
        [FromBody] ConfirmFinalAcceptanceRequest request, CancellationToken cancellationToken)
    {
        var result = await contractService.ConfirmFinalAcceptanceAsync(CurrentUserId, contractId, request, cancellationToken);
        return Success(result, "Contract finalized and accepted successfully.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}
