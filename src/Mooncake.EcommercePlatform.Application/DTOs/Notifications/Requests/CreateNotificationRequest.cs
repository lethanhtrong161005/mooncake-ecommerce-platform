namespace Mooncake.EcommercePlatform.Application.DTOs.Notifications.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload to create an in-app notification.</summary>
public record CreateNotificationRequest
{
    [Required]
    public long UserId { get; init; }

    [Required]
    [MaxLength(100)]
    public string Type { get; init; } = "general";

    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Body { get; init; }

    [MaxLength(50)]
    public string? EntityType { get; init; }

    public long? EntityId { get; init; }
}
