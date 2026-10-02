using Mooncake.EcommercePlatform.Domain.Common;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class AiConversation : BaseEntity
{
    public long UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public ICollection<AiMessage> AiMessages { get; set; } = [];
}
