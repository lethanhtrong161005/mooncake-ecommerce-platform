namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>A single private conversation between one customer and one supplier shop.</summary>
public class ChatThread : BaseEntity
{
    /// <summary>User who started the conversation from the storefront.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Owner user of the supplier shop.</summary>
    public Guid SupplierId { get; set; }

    public Guid ShopId { get; set; }

    public Guid? LastMessageId { get; set; }

    public Guid? LastMessageSenderId { get; set; }

    public string? LastMessagePreview { get; set; }

    /// <summary>Activity timestamp used for ordering; equals creation time until the first message.</summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>Participant who blocked this thread, or null when active.</summary>
    public Guid? BlockedByUserId { get; set; }
}

