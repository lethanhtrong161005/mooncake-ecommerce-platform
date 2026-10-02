namespace Mooncake.EcommercePlatform.Application.DTOs.Admin.Responses;

/// <summary>Supplier details returned to callers.</summary>
public record SupplierResponse(
    long Id,
    long UserId,
    string BusinessName,
    string? Description,
    string? Address,
    string? TaxCode,
    bool IsVerified,
    int ReputationScore,
    string UserEmail,
    string UserFullName,
    string? UserPhone,
    DateTime CreatedAtUtc
);
