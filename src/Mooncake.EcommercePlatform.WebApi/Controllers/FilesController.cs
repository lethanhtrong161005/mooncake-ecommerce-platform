namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;

/// <summary>File upload endpoints for images, packaging logos, and documents.</summary>
[Route("api/v1/files")]
public class FilesController(IFileStorageService fileStorageService) : BaseApiController
{
    /// <summary>Uploads a single file.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new HttpException(400, "Please provide a valid non-empty file.");
        }

        // Limit file size to 10MB
        if (file.Length > 10 * 1024 * 1024)
        {
            throw new HttpException(400, "File size must not exceed 10 MB.");
        }

        await using var stream = file.OpenReadStream();
        var fileUrl = await fileStorageService.SaveFileAsync(stream, file.FileName, file.ContentType, cancellationToken);

        var response = new FileUploadResponse(fileUrl, file.FileName, file.Length, file.ContentType);
        return Created(response, "File uploaded successfully.");
    }

    /// <summary>Uploads multiple files.</summary>
    [HttpPost("upload-multiple")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleAsync(List<IFormFile>? files, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            throw new HttpException(400, "Please provide at least one file.");
        }

        var results = new List<FileUploadResponse>();
        foreach (var file in files)
        {
            if (file.Length > 0 && file.Length <= 10 * 1024 * 1024)
            {
                await using var stream = file.OpenReadStream();
                var fileUrl = await fileStorageService.SaveFileAsync(stream, file.FileName, file.ContentType, cancellationToken);
                results.Add(new FileUploadResponse(fileUrl, file.FileName, file.Length, file.ContentType));
            }
        }

        return Created(results, "Files uploaded successfully.");
    }
}
