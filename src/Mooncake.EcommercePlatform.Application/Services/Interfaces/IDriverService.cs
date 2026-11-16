namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Responses;

/// <summary>Driver account registration, trip browsing, and profile management use cases.</summary>
public interface IDriverService
{
    Task<AuthenResponse> RegisterAsync(DriverRegisterRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PublicDriverDeliveryResponse>> GetTripsAsync(
        Guid userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PublicDriverDeliveryResponse> GetTripByShareCodeAsync(
        Guid userId, string shareCode, CancellationToken cancellationToken = default);

    Task<PublicDriverDeliveryResponse> ClaimTripAsync(
        Guid userId, Guid deliveryId, DriverClaimTripRequest request, CancellationToken cancellationToken = default);

    Task<DriverProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DriverProfileResponse> UpdateProfileAsync(
        Guid userId, UpdateDriverProfileRequest request, CancellationToken cancellationToken = default);
}
