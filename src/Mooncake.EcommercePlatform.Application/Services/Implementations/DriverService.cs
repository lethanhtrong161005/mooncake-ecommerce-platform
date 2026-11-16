namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Models;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Driver business workflow service handling registration, trip claiming, and profile updates.</summary>
public class DriverService(
    IDriverRepository driverRepository,
    IDeliveryRepository deliveryRepository,
    IUserRepository userRepository,
    IAuthService authService,
    IDateTimeProvider clock,
    INotificationService? notificationService = null) : IDriverService
{
    public async Task<AuthenResponse> RegisterAsync(DriverRegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new HttpException(400, "Thông tin đăng ký không hợp lệ. Vui lòng gửi lại.", OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new HttpException(400, "Số điện thoại không được để trống. Vui lòng nhập số điện thoại hợp lệ.", OrderErrorCodes.ValidationError);

        var normalizedPhone = PhoneNumberUtils.Normalize(request.PhoneNumber);
        if (!PhoneNumberUtils.IsValid(normalizedPhone))
            throw new HttpException(400, "Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại 10 chữ số hợp lệ.", OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new HttpException(400, "Họ và tên không được để trống. Vui lòng nhập họ và tên của bạn.", OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new HttpException(400, "Mật khẩu phải có ít nhất 8 ký tự. Vui lòng nhập mật khẩu hợp lệ.", OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.Token))
            throw new HttpException(400, "Mã token chuyến đi không được để trống. Vui lòng cung cấp mã chuyến hợp lệ.", OrderErrorCodes.ValidationError);

        var existingUser = await userRepository.GetByPhoneAsync(normalizedPhone, cancellationToken);
        if (existingUser is not null)
            throw new HttpException(409, "Số điện thoại này đã có tài khoản. Hãy đăng nhập.", OrderErrorCodes.StateConflict);

        var delivery = await deliveryRepository.GetByShareTokenAsync(request.Token.Trim(), cancellationToken);
        if (delivery is null)
            throw new HttpException(400, "Mã token chuyến đi không hợp lệ. Vui lòng kiểm tra lại liên kết nhận chuyến.", OrderErrorCodes.ValidationError);

        if (!delivery.DriverClaimedAt.HasValue)
            throw new HttpException(400, "Chuyến đi chưa được nhận bằng số điện thoại này. Vui lòng nhận chuyến trước khi tạo tài khoản.", OrderErrorCodes.ValidationError);

        var claimedPhone = PhoneNumberUtils.Normalize(delivery.DriverPhone ?? string.Empty);
        if (!string.Equals(claimedPhone, normalizedPhone, StringComparison.Ordinal))
            throw new HttpException(400, "Số điện thoại đăng ký không trùng khớp với số điện thoại đã nhận chuyến.", OrderErrorCodes.ValidationError);

        if (clock.UtcNow - delivery.DriverClaimedAt.Value > TimeSpan.FromHours(24))
            throw new HttpException(400, "Chuyến đi đã nhận quá 24 giờ. Vui lòng nhận chuyến mới để tạo tài khoản.", OrderErrorCodes.ValidationError);

        var now = clock.UtcNow;
        var authUserData = new AuthUserData
        {
            Id = Guid.NewGuid(),
            Role = UserRole.Driver,
            RequestedRole = UserRole.Driver,
            FullName = request.FullName.Trim(),
            Phone = normalizedPhone,
            Email = $"{normalizedPhone}@driver.mooncake.vn",
            PasswordHash = PasswordHashUtils.Hash(request.Password),
            IsActive = true,
            EmailVerifiedAt = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var createdUser = await userRepository.CreateAsync(authUserData, cancellationToken);

        var vehiclePlate = string.IsNullOrWhiteSpace(request.VehiclePlate)
            ? delivery.VehiclePlate
            : request.VehiclePlate.Trim();

        var driverProfile = new DriverProfile
        {
            Id = Guid.NewGuid(),
            UserId = createdUser.Id,
            FullName = createdUser.FullName ?? request.FullName.Trim(),
            Phone = normalizedPhone,
            DefaultVehiclePlate = vehiclePlate,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
        await driverRepository.CreateProfileAsync(driverProfile, cancellationToken);

        if (string.IsNullOrWhiteSpace(delivery.VehiclePlate) && !string.IsNullOrWhiteSpace(vehiclePlate))
        {
            delivery.VehiclePlate = vehiclePlate;
            delivery.UpdatedAt = now;
            await deliveryRepository.UpdateAsync(delivery, cancellationToken);
        }

        return await authService.LoginAsync(new LoginRequest(normalizedPhone, request.Password), cancellationToken);
    }

    public async Task<PagedResult<PublicDriverDeliveryResponse>> GetTripsAsync(
        Guid userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var driverProfile = await driverRepository.GetProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy hồ sơ tài xế.");

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var tripsPage = await driverRepository.GetDriverTripsPageAsync(
            driverProfile.Phone, status, safePage, safePageSize, cancellationToken);

        var responses = new List<PublicDriverDeliveryResponse>(tripsPage.Items.Count);
        foreach (var delivery in tripsPage.Items)
        {
            // Refresh an expired link only for trips still in progress; never reopen a link the supplier revoked.
            if (!delivery.ShareTokenRevokedAtUtc.HasValue &&
                IsActiveTrip(delivery.Status) &&
                (string.IsNullOrWhiteSpace(delivery.ShareToken) ||
                 !delivery.ShareTokenExpiresAtUtc.HasValue ||
                 delivery.ShareTokenExpiresAtUtc.Value <= clock.UtcNow))
            {
                delivery.ShareToken = DriverHelper.GenerateShareToken();
                delivery.ShareTokenCreatedAtUtc = clock.UtcNow;
                delivery.ShareTokenExpiresAtUtc = clock.UtcNow.AddHours(24);
                await deliveryRepository.UpdateAsync(delivery, cancellationToken);
            }

            var publicResponse = await DriverHelper.BuildPublicDriverResponseAsync(delivery, deliveryRepository, cancellationToken);
            responses.Add(publicResponse);
        }

        return new PagedResult<PublicDriverDeliveryResponse>(responses, tripsPage.Page, tripsPage.PageSize, tripsPage.TotalCount);
    }

    public async Task<PublicDriverDeliveryResponse> GetTripByShareCodeAsync(
        Guid userId, string shareCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            throw new HttpException(404, DeliveryMessages.ShareLinkNotFound);

        var delivery = await deliveryRepository.GetByShareCodeAsync(shareCode.Trim(), cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.ShareLinkNotFound);

        if (delivery.ShareTokenRevokedAtUtc.HasValue)
            throw new HttpException(404, DeliveryMessages.ShareLinkRevokedError, OrderErrorCodes.StateConflict);

        if (!delivery.ShareTokenExpiresAtUtc.HasValue || delivery.ShareTokenExpiresAtUtc.Value <= clock.UtcNow)
            throw new HttpException(404, DeliveryMessages.ShareLinkExpiredError, OrderErrorCodes.StateConflict);

        return await DriverHelper.BuildPublicDriverResponseAsync(delivery, deliveryRepository, cancellationToken);
    }

    public async Task<PublicDriverDeliveryResponse> ClaimTripAsync(
        Guid userId, Guid deliveryId, DriverClaimTripRequest request, CancellationToken cancellationToken = default)
    {
        var driverProfile = await driverRepository.GetProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy hồ sơ tài xế.");

        var delivery = await deliveryRepository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.DeliveryNotFound);

        if (delivery.Status is DeliveryStatus.Delivered or DeliveryStatus.Returned)
            throw new HttpException(409, DeliveryMessages.DeliveryAlreadyCompleted, OrderErrorCodes.StateConflict);

        var driverPhone = PhoneNumberUtils.Normalize(driverProfile.Phone);

        // A logged-in driver may only claim a trip whose share link is still open, or one the supplier
        // assigned to this driver's phone. A link the supplier revoked can never be claimed or reopened.
        if (delivery.ShareTokenRevokedAtUtc.HasValue)
            throw new HttpException(409, DeliveryMessages.ShareLinkRevokedError, OrderErrorCodes.StateConflict);

        var assignedToThisDriver = string.Equals(
            PhoneNumberUtils.Normalize(delivery.DriverPhone ?? string.Empty), driverPhone, StringComparison.Ordinal);
        var linkOpen = !string.IsNullOrWhiteSpace(delivery.ShareToken)
            && delivery.ShareTokenExpiresAtUtc.HasValue
            && delivery.ShareTokenExpiresAtUtc.Value > clock.UtcNow;
        if (!linkOpen && !assignedToThisDriver)
            throw new HttpException(409, DeliveryMessages.ShareLinkExpiredError, OrderErrorCodes.StateConflict);

        if (delivery.DriverClaimedAt.HasValue)
        {
            var existingPhone = PhoneNumberUtils.Normalize(delivery.DriverPhone ?? string.Empty);
            if (!string.Equals(existingPhone, driverPhone, StringComparison.Ordinal))
            {
                throw new HttpException(409, "Chuyến đã có tài xế khác nhận. Liên hệ xưởng nếu cần đổi tài xế.", OrderErrorCodes.StateConflict);
            }

            if (!string.IsNullOrWhiteSpace(request?.VehiclePlate))
            {
                delivery.VehiclePlate = request.VehiclePlate.Trim();
            }
            else if (string.IsNullOrWhiteSpace(delivery.VehiclePlate) && !string.IsNullOrWhiteSpace(driverProfile.DefaultVehiclePlate))
            {
                delivery.VehiclePlate = driverProfile.DefaultVehiclePlate.Trim();
            }
            delivery.DriverName = driverProfile.FullName;
            delivery.UpdatedAt = clock.UtcNow;
            var updatedExisting = await deliveryRepository.UpdateAsync(delivery, cancellationToken);
            return await DriverHelper.BuildPublicDriverResponseAsync(updatedExisting, deliveryRepository, cancellationToken);
        }

        delivery.DriverName = driverProfile.FullName;
        delivery.DriverPhone = driverPhone;
        delivery.VehiclePlate = string.IsNullOrWhiteSpace(request?.VehiclePlate)
            ? (driverProfile.DefaultVehiclePlate ?? string.Empty)
            : request.VehiclePlate.Trim();
        delivery.DriverClaimedAt = clock.UtcNow;
        delivery.UpdatedAt = clock.UtcNow;

        if (!linkOpen)
        {
            delivery.ShareToken = DriverHelper.GenerateShareToken();
            delivery.ShareTokenCreatedAtUtc = clock.UtcNow;
            delivery.ShareTokenExpiresAtUtc = clock.UtcNow.AddHours(24);
        }

        var updated = await deliveryRepository.UpdateAsync(delivery, cancellationToken);

        await deliveryRepository.AddWorkflowEventAsync(new WorkflowEvent
        {
            AggregateType = "Delivery",
            AggregateId = updated.Id,
            EventType = "DriverClaimed",
            FromStatus = updated.Status.ToString(),
            ToStatus = updated.Status.ToString(),
            ActorUserId = userId,
            Reason = $"Tài xế {updated.DriverName} – {updated.DriverPhone} – {updated.VehiclePlate} đã nhận chuyến"
        }, cancellationToken);

        if (notificationService is not null)
        {
            var supplierUserId = await deliveryRepository.GetSupplierUserIdForDeliveryAsync(updated.Id, cancellationToken);
            var customerUserId = await deliveryRepository.GetCustomerUserIdForDeliveryAsync(updated.Id, cancellationToken);
            var claimMessage = $"Tài xế {updated.DriverName} – {updated.DriverPhone} – {updated.VehiclePlate} đã nhận chuyến";

            if (supplierUserId.HasValue)
            {
                await notificationService.DispatchNotificationAsync(
                    supplierUserId.Value,
                    "delivery.driver_claimed",
                    "Chuyến giao hàng đã có tài xế nhận",
                    claimMessage,
                    "Delivery",
                    updated.Id,
                    null,
                    "medium",
                    cancellationToken);
            }

            if (customerUserId.HasValue)
            {
                await notificationService.DispatchNotificationAsync(
                    customerUserId.Value,
                    "delivery.driver_claimed",
                    "Tài xế đã nhận chuyến giao hàng",
                    claimMessage,
                    "Delivery",
                    updated.Id,
                    null,
                    "medium",
                    cancellationToken);
            }
        }

        return await DriverHelper.BuildPublicDriverResponseAsync(updated, deliveryRepository, cancellationToken);
    }

    public async Task<DriverProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await driverRepository.GetProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy hồ sơ tài xế.");
        return DriverHelper.ToProfileResponse(profile);
    }

    public async Task<DriverProfileResponse> UpdateProfileAsync(
        Guid userId, UpdateDriverProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FullName))
            throw new HttpException(400, "Họ và tên không được để trống. Vui lòng nhập họ và tên của bạn.", OrderErrorCodes.ValidationError);

        var profile = await driverRepository.GetProfileByUserIdAsync(userId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy hồ sơ tài xế.");

        profile.FullName = request.FullName.Trim();
        profile.DefaultVehiclePlate = request.DefaultVehiclePlate?.Trim();
        profile.UpdatedAt = clock.UtcNow;

        var updated = await driverRepository.UpdateProfileAsync(profile, cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            user.FullName = profile.FullName;
            user.UpdatedAtUtc = clock.UtcNow;
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        return DriverHelper.ToProfileResponse(updated);
    }

    private static bool IsActiveTrip(DeliveryStatus status) =>
        status is not (DeliveryStatus.Delivered or DeliveryStatus.Returned or DeliveryStatus.Failed);
}
