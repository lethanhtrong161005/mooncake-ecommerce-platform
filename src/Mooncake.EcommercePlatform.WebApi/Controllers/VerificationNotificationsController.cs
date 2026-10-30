namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Dual-purpose notification controller handling both feature-scoped verification notifications and general platform real-time notifications.</summary>
[Authorize]
[Route("api/v1/notifications")]
[EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VerificationNotificationsController(
    IVerificationNotificationService verificationService,
    INotificationService notificationService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] bool? isRead = null,
        CancellationToken ct = default)
    {
        if (category == C.Category)
        {
            return Success(await verificationService.InboxAsync(UserId, page, pageSize, ct), C.Success);
        }

        var result = await notificationService.GetInboxAsync(UserId, page, pageSize, category, isRead, ct);
        return Success(result, "Lấy danh sách thông báo thành công.");
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> Unread([FromQuery] string? category = null, CancellationToken ct = default)
    {
        if (category == C.Category)
        {
            var inbox = await verificationService.InboxAsync(UserId, 1, 1, ct);
            return Success(new { inbox.UnreadCount, inbox.ReadThroughToken }, C.Success);
        }

        var result = await notificationService.GetUnreadCountAsync(UserId, ct);
        return Success(result, "Lấy số lượng thông báo chưa đọc thành công.");
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        try
        {
            await verificationService.ReadAsync(UserId, id, ct);
        }
        catch
        {
            // If not found in verification notifications, try general notification service
            await notificationService.MarkAsReadAsync(UserId, id, ct);
        }
        return Success(C.Success);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll([FromBody] ReadGeneralOrVerificationNotificationRequest request, CancellationToken ct)
    {
        if (request.Category == C.Category)
        {
            SupplierVerificationHelper.Require(!string.IsNullOrEmpty(request.ReadThroughToken));
            await verificationService.ReadAllAsync(UserId, request.ReadThroughToken!, Request.Headers["Idempotency-Key"].ToString(), ct);
            return Success(C.Success);
        }

        var updated = await notificationService.MarkAllAsReadAsync(UserId, request.Category, request.ReadThroughTimestamp, ct);
        return Success(new { updatedCount = updated, unreadCount = 0 }, "Đã đánh dấu tất cả thông báo là đã đọc.");
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await notificationService.GetPreferencesAsync(UserId, ct);
        return Success(result, "Lấy cấu hình thông báo thành công.");
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferencesDto preferences, CancellationToken ct)
    {
        var result = await notificationService.UpdatePreferencesAsync(UserId, preferences, ct);
        return Success(result, "Cập nhật cấu hình thông báo thành công.");
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record ReadGeneralOrVerificationNotificationRequest(
    string? ReadThroughToken = null,
    string? Category = "all",
    DateTime? ReadThroughTimestamp = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReadVerificationNotificationsRequest(
    [Required] string ReadThroughToken, string Category = C.Category);
