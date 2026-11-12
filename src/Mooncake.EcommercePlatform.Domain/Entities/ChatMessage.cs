namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>A message inside a customer-supplier chat thread.</summary>
public class ChatMessage : BaseEntity
{
    public Guid ThreadId { get; set; }

    public Guid SenderId { get; set; }

    public ChatSenderRole SenderRole { get; set; }

    public ChatMessageType Type { get; set; }

    public string? Body { get; set; }

    /// <summary>Client-generated idempotency key; unique per sender within a thread.</summary>
    public Guid ClientMessageId { get; set; }

    public Guid? ProductId { get; set; }

    /// <summary>Product name snapshot at send time.</summary>
    public string? ProductName { get; set; }

    public string? ProductImageUrl { get; set; }

    public decimal? ProductPrice { get; set; }

    /// <summary>Set when the sender edits the text body within the allowed edit window.</summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>Set when the sender recalls the message; content is hidden from both participants.</summary>
    public DateTime? RecalledAt { get; set; }
}

