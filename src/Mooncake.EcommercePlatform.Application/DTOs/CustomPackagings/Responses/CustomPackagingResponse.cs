namespace Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Responses;

/// <summary>Custom packaging design details returned to callers.</summary>
public record CustomPackagingResponse(
    long Id,
    long CustomerId,
    string Name,
    string LogoUrl,
    string? DesignNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
