namespace Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record CreateReviewRequest
{
    public Guid? OrderId { get; init; }
    public Guid? ContractId { get; init; }
    [Range(1, 5)] public short OverallRating { get; init; }
    [Range(1, 5)] public short QualityRating { get; init; }
    [Range(1, 5)] public short DeliveryRating { get; init; }
    [Range(1, 5)] public short ServiceRating { get; init; }
    [MaxLength(5000)] public string? Comment { get; init; }
    [MaxLength(10)] public List<Uri> MediaUrls { get; init; } = [];
    public bool IsAnonymous { get; init; }
}

public sealed record ReplyToReviewRequest([Required, MinLength(1), MaxLength(2000)] string Reply);
public sealed record ModerateReviewRequest(bool IsPublic, [MaxLength(1000)] string? Reason);

public sealed record CreateProductReviewRequest
{
    [Required] public Guid OrderItemId { get; init; }
    [Range(1, 5)] public short Rating { get; init; }
    [MaxLength(255)] public string? Title { get; init; }
    [MaxLength(5000)] public string? Content { get; init; }
    [MaxLength(10)] public List<Uri> MediaUrls { get; init; } = [];
}
