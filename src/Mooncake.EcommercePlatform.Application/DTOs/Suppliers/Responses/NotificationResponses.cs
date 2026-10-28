namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

/// <summary>Own notification metadata without legal document content or review reasons.</summary>
public sealed record VerificationNotificationResponse(Guid Id, string? Type, string? Title, string? Content,
    Guid? ReferenceId, Guid? SourceEventId, long? AggregateVersion, long? Sequence, bool IsRead, DateTime CreatedAtUtc);
public sealed record VerificationInboxResponse(IReadOnlyList<VerificationNotificationResponse> Items,
    int Page, int PageSize, int TotalCount, int UnreadCount, string ReadThroughToken);
public sealed record VerificationInboxData(IReadOnlyList<VerificationNotificationResponse> Items, int TotalCount, int UnreadCount);
public sealed record VerificationReadBoundary(Guid UserId, string Category, long Sequence, DateTime ExpiresAtUtc);
