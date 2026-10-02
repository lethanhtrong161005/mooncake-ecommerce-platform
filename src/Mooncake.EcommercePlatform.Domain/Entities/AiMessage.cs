using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class AiMessage : BaseEntity
{
    public long ConversationId { get; set; }
    public AiMessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }

    public AiConversation Conversation { get; set; } = null!;
}
