namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.WebApi.Filters;

/// <summary>Driver account, trip management, and profile endpoints.</summary>
[Route("api/v1/driver")]
[Authorize(Roles = "Driver")]
[OrderApi]
public sealed class DriverController(IDriverService driverService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] DriverRegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await driverService.RegisterAsync(request, cancellationToken);
        return Success(result, "Đăng ký tài khoản tài xế thành công.");
    }

    [HttpGet("trips")]
    public async Task<IActionResult> GetTripsAsync(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await driverService.GetTripsAsync(CurrentUserId, status, page, pageSize, cancellationToken);
        return Success(result, DeliveryMessages.DeliveriesRetrieved);
    }

    [HttpGet("trips/code/{shareCode}")]
    public async Task<IActionResult> GetTripByShareCodeAsync(
        string shareCode, CancellationToken cancellationToken)
    {
        var result = await driverService.GetTripByShareCodeAsync(CurrentUserId, shareCode, cancellationToken);
        return Success(result, DeliveryMessages.DeliveryRetrieved);
    }

    [HttpPost("trips/{deliveryId:guid}/claim")]
    public async Task<IActionResult> ClaimTripAsync(
        Guid deliveryId,
        [FromBody] DriverClaimTripRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await driverService.ClaimTripAsync(
            CurrentUserId, deliveryId, request ?? new DriverClaimTripRequest(), cancellationToken);
        return Success(result, DeliveryMessages.DriverClaimed);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var result = await driverService.GetProfileAsync(CurrentUserId, cancellationToken);
        return Success(result, "Lấy thông tin tài xế thành công.");
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfileAsync(
        [FromBody] UpdateDriverProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await driverService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return Success(result, "Cập nhật thông tin tài xế thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException(DeliveryMessages.ValidSessionRequired);
}
