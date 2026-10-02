namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to attach an image URL to a product.</summary>
public record CreateProductImageRequest
{
    [Required]
    [MaxLength(1000)]
    public string Url { get; init; } = string.Empty;

    public int SortOrder { get; init; } = 0;

    public bool IsPrimary { get; init; } = false;
}
