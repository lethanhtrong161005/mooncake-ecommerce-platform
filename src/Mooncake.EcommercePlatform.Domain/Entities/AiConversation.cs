namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the AiConversation domain entity.</summary>
public class AiConversation : BaseEntity
{
    public Guid UserId { get; set; }

    public string SessionToken { get; set; } = string.Empty;

    public DateTime? EndedAt { get; set; }

    public AiConversationType ConversationType { get; set; } = AiConversationType.General;
}
