namespace Mooncake.EcommercePlatform.WebApi.Realtime;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Server-to-user invalidation channel; clients cannot choose recipients or invoke business commands.</summary>
[Authorize]
public sealed class SupplierVerificationHub : Hub
{
}

/// <summary>Uses the stable, verified account identity instead of a client-supplied subscription key.</summary>
public sealed class VerificationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        Guid.TryParse(connection.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id.ToString() : null;
}

/// <summary>Host adapter keeps SignalR dependencies out of Application and Infrastructure.</summary>
public sealed class VerificationRealtimePublisher(IHubContext<SupplierVerificationHub> hub) : IVerificationRealtimePublisher
{
    public Task PublishAsync(IReadOnlyList<Guid> userIds, string eventName, object payload, CancellationToken cancellationToken) =>
        hub.Clients.Users(userIds.Distinct().Select(x => x.ToString()).ToArray()).SendAsync(eventName, payload, cancellationToken);
}
