namespace Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Role of the participant who authored a chat message.</summary>
public enum ChatSenderRole
{
    Customer,
    Supplier
}

/// <summary>Primary content kind of a chat message.</summary>
public enum ChatMessageType
{
    Text,
    Attachment,
    ProductCard
}

/// <summary>Kind of a file attached to a chat message.</summary>
public enum ChatAttachmentKind
{
    Image,
    File
}

