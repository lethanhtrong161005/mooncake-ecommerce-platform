namespace Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Public user fields returned with a new token pair.</summary>
public record AuthenUserResponse(Guid Id, string? Phone, string? FullName, UserRole Role,
    UserRole RequestedRole = UserRole.Customer, SupplierAccessResponse? Capabilities = null,
    SupplierVerificationSummary? SupplierVerification = null);

/// <summary>Capabilities derived from current account and verification state.</summary>
public sealed record SupplierAccessResponse(bool CanReadSupplierVerification, bool CanSubmitSupplierVerification,
    bool CanCreateShop, bool CanSubmitBid, bool CanFulfillExistingOrders);

/// <summary>Minimal verification state safe for the authenticated account response.</summary>
public sealed record SupplierVerificationSummary(Guid? ProfileId, string OnboardingState,
    SupplierVerificationStatus? VerificationStatus, long Version);

/// <summary>A newly issued access/refresh token pair.</summary>
public record AuthenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    AuthenUserResponse User);
