namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the Notification domain entity.</summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool IsRead { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? ReferenceType { get; set; }
}
