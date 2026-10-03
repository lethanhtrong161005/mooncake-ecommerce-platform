namespace Mooncake.EcommercePlatform.Application.DTOs.Admin.Requests;

/// <summary>Request to verify or reject a supplier.</summary>
public record VerifySupplierRequest(
    bool IsVerified,
    int? ReputationScoreAdjustment = null,
    string? Notes = null
);
