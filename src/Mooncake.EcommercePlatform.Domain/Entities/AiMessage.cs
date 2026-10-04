namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using Pgvector;

/// <summary>Represents the AiMessage domain entity.</summary>
public class AiMessage : BaseEntity
{
    public Guid ConversationId { get; set; }

    public string Content { get; set; } = string.Empty;

    public int? TokensUsed { get; set; }

    public string? ModelUsed { get; set; }

    public AiMessageRole Role { get; set; } = AiMessageRole.User;

    public Vector? Embedding { get; set; }
}
