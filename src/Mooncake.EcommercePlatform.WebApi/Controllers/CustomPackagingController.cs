namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Customer and supplier custom packaging collaboration endpoints.</summary>
[Authorize]
[OrderApi]
[Route("api/v1/orders/{orderId:guid}/custom-packaging")]
public sealed class CustomPackagingController(
    ICustomPackagingService service,
    ICloudinaryStorageService? cloudinaryStorage = null) : BaseApiController
{
    private const long MaxLogoBytes = 10 * 1024 * 1024;

    /// <summary>Uploads a customer corporate logo to Cloudinary for custom packaging.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpPost("/api/v1/custom-packaging/upload-logo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogoAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new HttpException(400, B2BOrderingMessages.LogoFileRequired, OrderErrorCodes.ValidationError);

        if (file.Length > MaxLogoBytes)
            throw new HttpException(400, B2BOrderingMessages.LogoFileTooLarge, OrderErrorCodes.ValidationError);

        if (cloudinaryStorage is null)
            throw new HttpException(503, B2BOrderingMessages.LogoStorageUnavailable, OrderErrorCodes.FeatureDisabled);

        await using var stream = file.OpenReadStream();
        var upload = await cloudinaryStorage.UploadImageAsync(stream, file.FileName, false, cancellationToken);
        return Success(new { url = upload.Url, publicId = upload.PublicId }, B2BOrderingMessages.LogoUploaded);
    }

    /// <summary>Creates the custom packaging brief for a customer order.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(Guid orderId,
        [FromBody] CreateCustomPackagingRequest request, CancellationToken cancellationToken) =>
        Created(await service.CreateAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            B2BOrderingMessages.PackagingCreated);

    /// <summary>Returns a packaging brief and its complete revision timeline.</summary>
    [Authorize(Roles = "Customer,Supplier,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(User.IsInRole("Admin")
                ? await service.GetAsync(GetCurrentUserId(), false, orderId, true, cancellationToken)
                : await service.GetAsync(GetCurrentUserId(), IsSupplier(), orderId, cancellationToken),
            B2BOrderingMessages.PackagingLoaded);

    /// <summary>Submits a supplier mockup revision for customer review.</summary>
    [Authorize(Policy = "SupplierExistingFulfillment")]
    [HttpPost("revisions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitRevisionAsync(Guid orderId,
        [FromBody] SubmitPackagingRevisionRequest request, CancellationToken cancellationToken) =>
        Created(await service.SubmitRevisionAsync(GetCurrentUserId(), orderId, request, cancellationToken),
            B2BOrderingMessages.PackagingRevisionSubmitted);

    /// <summary>Approves a supplier mockup or asks for another revision.</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("revisions/{revisionId:guid}/review")]
    public async Task<IActionResult> ReviewRevisionAsync(Guid orderId, Guid revisionId,
        [FromBody] ReviewPackagingRevisionRequest request, CancellationToken cancellationToken) =>
        Success(await service.ReviewRevisionAsync(GetCurrentUserId(), orderId, revisionId, request,
            cancellationToken), B2BOrderingMessages.PackagingRevisionReviewed);

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsSupplier() => User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true");
}
