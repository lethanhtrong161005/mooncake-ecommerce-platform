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
    private const long MaxContractUploadBytes = 10 * 1024 * 1024;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await contractService.GetMineAsync(CurrentUserId, User.IsInRole("Supplier"), cancellationToken), "Contracts retrieved successfully.");

    [HttpGet("{contractId:guid}")]
    public async Task<IActionResult> GetAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.GetAsync(CurrentUserId, contractId, cancellationToken), "Contract retrieved successfully.");

    [HttpPost("{contractId:guid}/signature-otp")]
    public async Task<IActionResult> RequestSignatureOtpAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.RequestSignatureOtpAsync(CurrentUserId, contractId, cancellationToken), "A signature code was sent to your verified email.");

    [HttpPost("{contractId:guid}/submit-for-signature")]
    public async Task<IActionResult> BeginSignatureAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.BeginSignatureAsync(CurrentUserId, contractId, cancellationToken), "Contract is pending signature.");

    [HttpPost("{contractId:guid}/document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxContractUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadDocumentAsync(
        Guid contractId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await contractService.UploadDocumentAsync(CurrentUserId, contractId, stream,
            file.FileName, file.ContentType, file.Length, cancellationToken);
        return Success(result, "Đã lưu file hợp đồng lên kho bảo mật.");
    }

    [HttpGet("{contractId:guid}/document")]
    public async Task<IActionResult> GetDocumentAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await contractService.GetDocumentDownloadAsync(CurrentUserId, contractId, cancellationToken),
            "Đã tạo liên kết tải hợp đồng.");

    [HttpPost("{contractId:guid}/sign")]
    public async Task<IActionResult> SignAsync(Guid contractId, [FromBody] SignContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contractService.SignAsync(CurrentUserId, contractId, request.OtpCode, request.ConfirmTerms,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return Success(result, "Contract signature recorded successfully.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}
