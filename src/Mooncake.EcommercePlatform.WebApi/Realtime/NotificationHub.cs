namespace Mooncake.EcommercePlatform.WebApi.Realtime;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Two-way real-time notification hub for all authenticated users across Customer, Supplier, and Admin roles.</summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrEmpty(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{role}Group");
        }
        await base.OnConnectedAsync();
    }

    public Task JoinContractGroup(string contractId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"Contract_{contractId}");

    public Task LeaveContractGroup(string contractId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Contract_{contractId}");
}

/// <summary>Adapter implementation of IRealtimeNotificationPublisher using ASP.NET Core SignalR.</summary>
public sealed class SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext) : IRealtimeNotificationPublisher
{
    public Task PublishToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(userId.ToString()).SendAsync(eventName, payload, cancellationToken);

    public Task PublishToUsersAsync(IReadOnlyList<Guid> userIds, string eventName, object payload, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Users(userIds.Distinct().Select(x => x.ToString()).ToArray()).SendAsync(eventName, payload, cancellationToken);

    public Task PublishToGroupAsync(string groupName, string eventName, object payload, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
}

