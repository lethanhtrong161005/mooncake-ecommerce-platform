namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Per-participant read position and denormalized unread counter of a chat thread.</summary>
public class ChatParticipantState : BaseEntity
{
    public Guid ThreadId { get; set; }

    public Guid UserId { get; set; }

    public Guid? LastReadMessageId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public int UnreadCount { get; set; }
}

