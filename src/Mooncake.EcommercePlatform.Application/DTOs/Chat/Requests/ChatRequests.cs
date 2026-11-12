namespace Mooncake.EcommercePlatform.Application.DTOs.Chat.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;

/// <summary>Opens (or returns the existing) conversation between the current user and a shop/supplier.</summary>
public sealed record OpenChatThreadRequest : IValidatableObject
{
    public Guid? ShopId { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid TargetId => ShopId is { } shopId && shopId != Guid.Empty ? shopId : SupplierId ?? Guid.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TargetId == Guid.Empty)
            yield return new ValidationResult(ChatConstants.InvalidShopId, [nameof(ShopId)]);
    }
}

/// <summary>Cursor pagination query shared by thread and message listings.</summary>
public sealed record ChatPageQuery : IValidatableObject
{
    [Range(1, ChatConstants.MaxPageSize, ErrorMessage = ChatConstants.InvalidPageSize)]
    public int PageSize { get; init; } = ChatConstants.DefaultPageSize;

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [MaxLength(ChatConstants.MaxCursorLength, ErrorMessage = ChatConstants.InvalidCursor)]
    public string? Cursor { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cursor is not null && string.IsNullOrWhiteSpace(Cursor))
            yield return new ValidationResult(ChatConstants.InvalidCursor, [nameof(Cursor)]);
    }
}

/// <summary>Sends text, a product card, and/or previously uploaded attachments.</summary>
public sealed record SendChatMessageRequest : IValidatableObject
{
    [NonEmptyGuid(ErrorMessage = ChatConstants.InvalidClientMessageId)]
    public Guid ClientMessageId { get; init; }

    [MaxLength(ChatConstants.MaxBodyLength, ErrorMessage = ChatConstants.BodyTooLong)]
    public string? Body { get; init; }

    public Guid? ProductId { get; init; }

    public Guid[]? AttachmentIds { get; init; }

    /// <summary>Body trimmed to null when it only contains whitespace.</summary>
    public string? NormalizedBody => string.IsNullOrWhiteSpace(Body) ? null : Body.Trim();

    public IReadOnlyList<Guid> NormalizedAttachmentIds => AttachmentIds ?? [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var attachments = AttachmentIds ?? [];

        if (NormalizedBody is null && attachments.Length == 0 && ProductId is null)
            yield return new ValidationResult(ChatConstants.EmptyMessage, [nameof(Body)]);

        if (ProductId == Guid.Empty)
            yield return new ValidationResult(ChatConstants.InvalidProductId, [nameof(ProductId)]);

        if (attachments.Length > ChatConstants.MaxAttachmentsPerMessage)
            yield return new ValidationResult(ChatConstants.TooManyAttachments, [nameof(AttachmentIds)]);

        if (attachments.Any(id => id == Guid.Empty))
            yield return new ValidationResult(ChatConstants.InvalidAttachmentId, [nameof(AttachmentIds)]);
        else if (attachments.Distinct().Count() != attachments.Length)
            yield return new ValidationResult(ChatConstants.DuplicateAttachments, [nameof(AttachmentIds)]);
    }
}

/// <summary>Edits the text body of a previously sent message within the allowed window.</summary>
public sealed record EditChatMessageRequest : IValidatableObject
{
    [Required(ErrorMessage = ChatConstants.EmptyMessage)]
    [MaxLength(ChatConstants.MaxBodyLength, ErrorMessage = ChatConstants.BodyTooLong)]
    public string? Body { get; init; }

    public string NormalizedBody => (Body ?? string.Empty).Trim();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Body))
            yield return new ValidationResult(ChatConstants.EmptyMessage, [nameof(Body)]);
    }
}

/// <summary>Moves the caller's read marker forward to the given message.</summary>
public sealed record MarkChatReadRequest
{
    [NonEmptyGuid(ErrorMessage = ChatConstants.InvalidMessageId)]
    public Guid MessageId { get; init; }
}

/// <summary>Batch presence lookup for chat partners (comma-separated or repeated ids).</summary>
public sealed record ChatPresenceQuery : IValidatableObject
{
    [Required(ErrorMessage = ChatConstants.InvalidPresenceIds)]
    public string[]? UserIds { get; init; }

    public IReadOnlyList<Guid> ToUserIds() =>
        (UserIds ?? [])
            .SelectMany(raw => raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(Guid.Parse)
            .ToArray();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var parts = (UserIds ?? [])
            .SelectMany(raw => (raw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();

        var parsed = parts
            .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
            .ToArray();

        if (parsed.Length is 0 or > ChatConstants.MaxPresenceIds
            || parsed.Any(id => id == Guid.Empty)
            || parsed.Distinct().Count() != parsed.Length)
        {
            yield return new ValidationResult(ChatConstants.InvalidPresenceIds, [nameof(UserIds)]);
        }
    }
}

/// <summary>Privacy preference for showing the caller's active status to chat partners.</summary>
public sealed record UpdateChatPresenceSettingsRequest
{
    [Required]
    public bool? ShowActiveStatus { get; init; }
}

/// <summary>A validated upload item handed from the host to the application layer.</summary>
public sealed record ChatUploadItem(
    Func<Stream> OpenReadStream,
    string FileName,
    string ContentType,
    long Length);

