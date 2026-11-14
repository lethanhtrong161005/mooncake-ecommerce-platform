namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Shipment scheduling, status updates, and Proof-of-Delivery endpoints.</summary>
[Route("api/v1/deliveries")]
[Authorize]
[OrderApi]
public sealed class DeliveriesController(
    IDeliveryService deliveryService,
    ICloudinaryStorageService? cloudinaryStorage = null) : BaseApiController
{
    private const long MaxDriverPhotoBytes = 10 * 1024 * 1024;
    [HttpGet]
    public async Task<IActionResult> GetMineAsync([FromQuery] DeliveryPageQueryRequest query,
        CancellationToken cancellationToken) =>
        Success<object>(Request.Query.ContainsKey("page")
            ? await deliveryService.GetMinePageAsync(CurrentUserId,
                User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true"), query, cancellationToken)
            : await deliveryService.GetMineAsync(CurrentUserId,
                User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true"), cancellationToken),
            DeliveryMessages.DeliveriesRetrieved);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetAsync(CurrentUserId, id, cancellationToken), DeliveryMessages.DeliveryRetrieved);

    [HttpGet("contract/{contractId:guid}")]
    public async Task<IActionResult> GetByContractAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetByContractAsync(CurrentUserId, contractId, cancellationToken), DeliveryMessages.ContractDeliveriesRetrieved);

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetByOrderAsync(CurrentUserId, orderId, cancellationToken), DeliveryMessages.OrderDeliveriesRetrieved);

    [HttpPost]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.CreateAsync(CurrentUserId, request, cancellationToken);
        return Created(result, DeliveryMessages.DeliveryCreated);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatusAsync(Guid id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.UpdateStatusAsync(CurrentUserId, id, request, cancellationToken);
        return Success(result, DeliveryMessages.DeliveryStatusUpdated);
    }

    [HttpPut("{id:guid}/carrier-info")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> UpdateCarrierInfoAsync(Guid id, [FromBody] UpdateCarrierInfoRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.UpdateCarrierInfoAsync(CurrentUserId, id, request, cancellationToken);
        return Success(result, DeliveryMessages.CarrierInfoUpdated);
    }

    [HttpPost("{id:guid}/proofs")]
    public async Task<IActionResult> SubmitProofAsync(Guid id, [FromBody] CreateDeliveryProofRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.SubmitProofAsync(CurrentUserId, id, request, cancellationToken);
        return Created(result, DeliveryMessages.ProofSubmitted);
    }

    [HttpGet("{id:guid}/proofs")]
    public async Task<IActionResult> GetProofsAsync(Guid id, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetProofsAsync(CurrentUserId, id, cancellationToken), DeliveryMessages.ProofsRetrieved);

    [HttpGet("proofs/{proofId:guid}")]
    public async Task<IActionResult> GetProofDetailAsync(Guid proofId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetProofDetailAsync(CurrentUserId, proofId, cancellationToken), DeliveryMessages.ProofRetrieved);

    [HttpGet("proofs/{proofId:guid}/verify")]
    public async Task<IActionResult> VerifyProofIntegrityAsync(Guid proofId, CancellationToken cancellationToken)
    {
        var isVerified = await deliveryService.VerifyProofIntegrityAsync(proofId, cancellationToken);
        return Success(new { isVerified }, DeliveryMessages.ProofVerified);
    }

    [HttpGet("{id:guid}/share-link")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> GetShareLinkAsync(Guid id, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetShareLinkAsync(CurrentUserId, id, cancellationToken),
            DeliveryMessages.ShareLinkRetrieved);

    [HttpPost("{id:guid}/share-link")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> CreateOrReshareLinkAsync(Guid id,
        [FromBody] CreateDeliveryShareLinkRequest? request, CancellationToken cancellationToken) =>
        Success(await deliveryService.CreateOrReshareLinkAsync(CurrentUserId, id,
                request ?? new CreateDeliveryShareLinkRequest(), cancellationToken),
            DeliveryMessages.ShareLinkCreated);

    [HttpDelete("{id:guid}/share-link")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> RevokeShareLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        await deliveryService.RevokeShareLinkAsync(CurrentUserId, id, cancellationToken);
        return Success(new { deliveryId = id, revoked = true }, DeliveryMessages.ShareLinkRevoked);
    }

    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<IActionResult> GetPublicByTokenAsync(string token, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetPublicByTokenAsync(token, cancellationToken),
            DeliveryMessages.DeliveryRetrieved);

    [AllowAnonymous]
    [HttpPost("public/{token}/proofs/upload-photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPublicProofPhotoAsync(string token, IFormFile? file, CancellationToken cancellationToken)
    {
        await deliveryService.EnsurePublicTokenSubmittableAsync(token, cancellationToken);

        if (file is null || file.Length == 0)
            throw new HttpException(400, B2BOrderingMessages.LogoFileRequired, OrderErrorCodes.ValidationError);

        if (file.Length > MaxDriverPhotoBytes)
            throw new HttpException(400, B2BOrderingMessages.LogoFileTooLarge, OrderErrorCodes.ValidationError);

        if (cloudinaryStorage is null)
            throw new HttpException(503, B2BOrderingMessages.LogoStorageUnavailable, OrderErrorCodes.FeatureDisabled);

        await using var stream = file.OpenReadStream();
        var upload = await cloudinaryStorage.UploadImageAsync(stream, file.FileName, false, cancellationToken);
        return Success(new { url = upload.Url, publicId = upload.PublicId }, DeliveryMessages.DriverPhotoUploaded);
    }

    [AllowAnonymous]
    [HttpPost("public/{token}/proofs")]
    public async Task<IActionResult> SubmitPublicProofAsync(string token,
        [FromBody] CreateDeliveryProofRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.SubmitPublicProofAsync(token, request, cancellationToken);
        return Created(result, DeliveryMessages.ProofSubmitted);
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException(DeliveryMessages.ValidSessionRequired);
}
