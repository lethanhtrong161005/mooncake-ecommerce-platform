namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>An uploaded image or document; pending until linked to a message by its uploader.</summary>
public class ChatAttachment : BaseEntity
{
    public Guid UploaderId { get; set; }

    public Guid? MessageId { get; set; }

    public ChatAttachmentKind Kind { get; set; }

    /// <summary>Public delivery URL for images; empty for private documents.</summary>
    public string Url { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int SortOrder { get; set; }
}

