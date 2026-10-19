namespace Mooncake.EcommercePlatform.WebApi.Hubs;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRNotificationSink.GetUserGroup(id));
        await base.OnConnectedAsync();
    }
}

public sealed class SignalRNotificationSink(IHubContext<NotificationHub> hub) : IRealtimeNotificationSink
{
    public Task PublishAsync(RealtimeNotificationEvent notification, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(GetUserGroup(notification.UserId)).SendAsync("notification", notification, cancellationToken);

    public static string GetUserGroup(Guid userId) => $"user:{userId:N}";
}
