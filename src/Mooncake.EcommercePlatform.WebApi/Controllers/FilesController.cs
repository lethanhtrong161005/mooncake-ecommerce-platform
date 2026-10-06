namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files;

[Authorize(Roles = "Customer,Supplier,Admin")]
[Route("api/v1/files")]
public sealed class FilesController(IFileStorageService fileStorageService) : BaseApiController
{
    [HttpPost("presigned-upload")]
    public IActionResult CreateUploadUrl([FromBody] PresignUploadRequest request) =>
        Success(fileStorageService.CreateUploadUrl(GetCurrentUserId(), request.FileName, request.ContentType, request.FileSizeBytes), "Upload URL created successfully.");

    [HttpPost("presigned-download")]
    public IActionResult CreateDownloadUrl([FromBody] PresignDownloadRequest request) =>
        Success(fileStorageService.CreateDownloadUrl(GetCurrentUserId(), request.ObjectKey, User.IsInRole("Admin")), "Download URL created successfully.");

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
