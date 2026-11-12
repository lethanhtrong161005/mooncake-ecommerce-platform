namespace Mooncake.EcommercePlatform.WebApi.DTOs.Requests;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;

/// <summary>Multipart upload request for chat files with validation on count, size, and extension.</summary>
public sealed record ChatFileUploadRequest : IValidatableObject
{
    public List<IFormFile>? Files { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var list = Files ?? [];
        if (list.Count == 0)
        {
            yield return new ValidationResult(ChatConstants.NoFiles, [nameof(Files)]);
            yield break;
        }

        if (list.Count > ChatConstants.MaxUploadFiles)
        {
            yield return new ValidationResult(ChatConstants.TooManyFiles, [nameof(Files)]);
            yield break;
        }

        long totalBytes = 0;
        foreach (var file in list)
        {
            if (file.Length <= 0)
            {
                yield return new ValidationResult(ChatConstants.InvalidFileContent, [nameof(Files)]);
                yield break;
            }

            if (file.Length > ChatConstants.MaxFileBytes)
            {
                yield return new ValidationResult(ChatConstants.FileTooLarge, [nameof(Files)]);
                yield break;
            }

            totalBytes += file.Length;
            if (totalBytes > ChatConstants.MaxTotalUploadBytes)
            {
                yield return new ValidationResult(ChatConstants.TotalTooLarge, [nameof(Files)]);
                yield break;
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)
                || (!ChatConstants.ImageExtensions.Contains(ext) && !ChatConstants.DocumentExtensions.Contains(ext)))
            {
                yield return new ValidationResult(ChatConstants.UnsupportedFile, [nameof(Files)]);
                yield break;
            }
        }
    }

    public IReadOnlyList<ChatUploadItem> ToUploadItems() =>
        (Files ?? [])
            .Select(f => new ChatUploadItem(
                () => f.OpenReadStream(),
                f.FileName,
                f.ContentType,
                f.Length))
            .ToArray();
}

