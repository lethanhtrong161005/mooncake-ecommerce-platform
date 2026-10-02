using Mooncake.EcommercePlatform.Domain.Common;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Notification : BaseEntity
{
    public long UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
