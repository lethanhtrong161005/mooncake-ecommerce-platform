namespace Mooncake.EcommercePlatform.WebApi.Realtime;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Real-time customer-supplier messaging and presence hub.</summary>
[Authorize]
public sealed class ChatHub(
    IPresenceTracker presence,
    IChatService chatService,
    ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChatConstants.UserGroup(userId));
            if (presence.Connect(userId, Context.ConnectionId))
            {
                _ = Task.Run(async () =>
                {
                    try { await chatService.NotifyUserOnlineAsync(userId); }
                    catch (Exception ex) { logger.LogWarning(ex, "Failed to publish online state for {UserId}", userId); }
                });
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatConstants.UserGroup(userId));
            if (presence.Disconnect(userId, Context.ConnectionId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(ChatConstants.PresenceOfflineGraceSeconds));
                        if (!presence.IsOnline(userId))
                            await chatService.NotifyUserOfflineAsync(userId);
                    }
                    catch (Exception ex) { logger.LogWarning(ex, "Failed to publish offline state for {UserId}", userId); }
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinThread(string threadIdString)
    {
        if (Guid.TryParse(threadIdString, out var threadId) && TryGetUserId(out var userId))
        {
            if (await chatService.IsThreadMemberAsync(userId, threadId))
                await Groups.AddToGroupAsync(Context.ConnectionId, ChatConstants.ThreadGroup(threadId));
        }
    }

    public Task LeaveThread(string threadIdString)
    {
        if (Guid.TryParse(threadIdString, out var threadId))
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatConstants.ThreadGroup(threadId));
        return Task.CompletedTask;
    }

    public async Task SendTyping(string threadIdString, bool isTyping)
    {
        if (Guid.TryParse(threadIdString, out var threadId) && TryGetUserId(out var userId))
        {
            if (await chatService.IsThreadMemberAsync(userId, threadId))
            {
                await Clients.OthersInGroup(ChatConstants.ThreadGroup(threadId))
                    .SendAsync(ChatConstants.EventTyping, new { threadId, userId, isTyping });
            }
        }
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId != Guid.Empty;
}

/// <summary>Pushes chat and presence events to users via SignalR.</summary>
public sealed class SignalRChatPublisher(IHubContext<ChatHub> hubContext) : IChatRealtimePublisher
{
    public Task PublishToUsersAsync(IReadOnlyList<Guid> userIds, string eventName, object payload,
        CancellationToken ct = default)
    {
        var stringIds = userIds.Distinct().Select(id => id.ToString()).ToArray();
        return hubContext.Clients.Users(stringIds).SendAsync(eventName, payload, ct);
    }
}

