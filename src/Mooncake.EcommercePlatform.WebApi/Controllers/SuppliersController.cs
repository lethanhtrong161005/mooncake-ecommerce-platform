namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Owner onboarding and administrator decisions for immutable supplier submissions.</summary>
[Authorize]
[Route("api/v1/suppliers")]
[EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SuppliersController(ISupplierService service) : BaseApiController
{
    [HttpPost("me/onboarding")]
    public async Task<IActionResult> StartOnboarding(CancellationToken ct) =>
        Success(await service.StartOnboardingAsync(UserId, ct), C.Success);

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Success(await service.GetMyProfileAsync(UserId, ct), C.Success);

    [HttpPost("me/apply")]
    public async Task<IActionResult> Apply([FromBody] ApplySupplierRequest request, CancellationToken ct) =>
        Success(await service.ApplyAsync(UserId, Key, request, ct), C.Success);

    [HttpPost("me/documents")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string documentType, [FromForm] DateOnly? expiresOn, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Created(await service.UploadAsync(UserId, Key, stream, file.FileName, documentType, expiresOn, ct), C.Success);
    }

    [HttpGet("me/documents/{documentId:guid}")]
    public async Task<IActionResult> Document(Guid documentId, CancellationToken ct) =>
        Success(await service.GetDocumentAsync(UserId, documentId, ct), C.Success);

    [HttpDelete("me/documents/{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken ct)
    {
        await service.DeleteDocumentAsync(UserId, documentId, Key, ct);
        return Success(C.Success);
    }

    [HttpGet("documents/{documentId:guid}/content")]
    public async Task<IActionResult> Content(Guid documentId, CancellationToken ct)
    {
        var file = await service.ReadDocumentAsync(UserId, documentId, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("me/history")]
    public async Task<IActionResult> MyHistory(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        Success(await service.HistoryAsync(UserId, null, page, pageSize, ct), C.Success);

    [HttpGet("me/submissions/{submissionId:guid}")]
    public async Task<IActionResult> MySubmission(Guid submissionId, CancellationToken ct) =>
        Success(await service.GetSubmissionAsync(UserId, null, submissionId, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpGet("{profileId:guid}/submissions/{submissionId:guid}")]
    public async Task<IActionResult> Submission(Guid profileId, Guid submissionId, CancellationToken ct) =>
        Success(await service.GetSubmissionAsync(UserId, profileId, submissionId, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpGet("applications")]
    public async Task<IActionResult> Applications([FromQuery] SupplierApplicationQuery query, CancellationToken ct) =>
        Success(await service.ListAsync(UserId, query, ct), C.Success);

    // Transitional path now returns the documented paged summary, never N+1 document detail rows.
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> Pending(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        Success(await service.ListAsync(UserId, new() { Page = page, PageSize = pageSize, Status = SupplierVerificationStatus.Pending }, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpGet("{profileId:guid}/verification")]
    public async Task<IActionResult> Detail(Guid profileId, CancellationToken ct) =>
        Success(await service.GetProfileAsync(UserId, profileId, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpGet("{profileId:guid}/history")]
    public async Task<IActionResult> History(Guid profileId, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        Success(await service.HistoryAsync(UserId, profileId, page, pageSize, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpPost("{profileId:guid}/review")]
    public async Task<IActionResult> Review(Guid profileId, [FromBody] ReviewSupplierRequest request, CancellationToken ct) =>
        Success(await service.ReviewAsync(profileId, UserId, Key, request, ct), C.Success);

    [Authorize(Roles = "Admin")]
    [HttpPost("{profileId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid profileId, [FromBody] RevokeSupplierRequest request, CancellationToken ct) =>
        Success(await service.RevokeAsync(profileId, UserId, Key, request, ct), C.Success);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Key => Request.Headers["Idempotency-Key"].ToString();
}
