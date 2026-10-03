namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>In-app notification endpoints.</summary>
[Route("api/v1/notifications")]
public class NotificationsController(INotificationService notificationService) : BaseApiController
{
    /// <summary>Gets all notifications for a specific user.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationsAsync([FromQuery] long userId, [FromQuery] bool? unreadOnly, CancellationToken cancellationToken)
    {
        var notifications = await notificationService.GetUserNotificationsAsync(userId, unreadOnly, cancellationToken);
        return Success(notifications, "Notifications retrieved successfully.");
    }

    /// <summary>Gets unread notification count for a user.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCountAsync([FromQuery] long userId, CancellationToken cancellationToken)
    {
        var count = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Success(new { unreadCount = count }, "Unread notification count retrieved successfully.");
    }

    /// <summary>Marks a specific notification as read.</summary>
    [HttpPut("{id:long}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(long id, CancellationToken cancellationToken)
    {
        var notification = await notificationService.MarkAsReadAsync(id, cancellationToken);
        return Success(notification, "Notification marked as read.");
    }

    /// <summary>Marks all notifications as read for a user.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsReadAsync([FromQuery] long userId, CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Success("All notifications marked as read.");
    }

    /// <summary>Dispatches a new notification.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNotificationAsync([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = await notificationService.CreateNotificationAsync(request, cancellationToken);
        return Created(notification, "Notification created successfully.");
    }
}
