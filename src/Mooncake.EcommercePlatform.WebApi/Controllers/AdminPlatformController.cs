namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminPlatformController(IAdminPlatformService adminService) : BaseApiController
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken) =>
        Success(await adminService.GetDashboardAsync(cancellationToken), "Admin dashboard retrieved successfully.");

    [HttpGet("system-config")]
    public async Task<IActionResult> GetSystemConfigsAsync(CancellationToken cancellationToken) =>
        Success(await adminService.GetSystemConfigsAsync(cancellationToken), "System configuration retrieved successfully.");

    [HttpPut("system-config/{key}")]
    public async Task<IActionResult> SetSystemConfigAsync(string key, [FromBody] SetSystemConfigRequest request, CancellationToken cancellationToken) =>
        Success(await adminService.SetSystemConfigAsync(key, request.ValueJson, request.Description, GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken), "System configuration updated successfully.");

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Success(await adminService.GetAuditLogsAsync(page, pageSize, cancellationToken), "Audit logs retrieved successfully.");

    [HttpPatch("users/{userId:guid}")]
    public async Task<IActionResult> UpdateUserAsync(Guid userId, [FromBody] AdminUserUpdateRequest request, CancellationToken cancellationToken)
    {
        await adminService.UpdateUserAsync(userId, request, GetCurrentUserId(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Success("User updated successfully.");
    }

    [HttpGet("shops")]
    public async Task<IActionResult> GetShopsAsync(CancellationToken cancellationToken) =>
        Success(await adminService.GetShopsAsync(cancellationToken), "Shops retrieved successfully.");

    [HttpPatch("shops/{shopId:guid}/status")]
    public async Task<IActionResult> SetShopStatusAsync(Guid shopId, [FromBody] SetShopStatusRequest request, CancellationToken cancellationToken)
    {
        await adminService.SetShopStatusAsync(shopId, request.Status, cancellationToken);
        return Success("Shop status updated successfully.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record SetSystemConfigRequest(string ValueJson, string? Description);
public sealed record SetShopStatusRequest(ShopStatus Status);
