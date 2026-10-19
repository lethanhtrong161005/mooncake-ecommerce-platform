namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : BaseApiController
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default) =>
        Success(await notificationService.GetMineAsync(CurrentUserId, unreadOnly, page, pageSize, cancellationToken),
            "Notifications retrieved successfully.");

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(CurrentUserId, notificationId, cancellationToken);
        return Success("Notification marked as read.");
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllReadAsync(CancellationToken cancellationToken) =>
        Success(new { UpdatedCount = await notificationService.MarkAllReadAsync(CurrentUserId, cancellationToken) },
            "Notifications marked as read.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}
