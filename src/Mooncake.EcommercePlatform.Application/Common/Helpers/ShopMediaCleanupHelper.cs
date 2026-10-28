namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Persistence;

/// <summary>Claims unreferenced assets under owner/media locks; external deletion never holds database locks.</summary>
public static class ShopMediaCleanupHelper
{
    public static async Task<int> RunAsync(IShopRepository repository, IShopMediaGateway media, IDateTimeProvider clock, CancellationToken ct)
    {
        var now = clock.UtcNow;
        await repository.DeleteExpiredReplaysAsync(new(now, 100), ct);
        var candidates = await repository.ListCleanupCandidatesAsync(new(now.AddHours(-24), now.AddMinutes(-5), 20), ct);
        var count = 0;
        foreach (var candidate in candidates)
        {
            var claim = await repository.InTransactionAsync(new(candidate.OwnerUserId, MediaId: candidate.Id), async (tx, token) => {
                var current = await tx.GetMediaAsync(new(candidate.Id), token);
                if (current == null || current.IsDeleted || current.UpdatedAtUtc > (current.Status == "Deleting" ? now.AddMinutes(-5) : now.AddHours(-24)) ||
                    await tx.IsMediaReferencedAsync(new(candidate.Id), token)) return null;
                var state = current with { Status = "Deleting", LeaseToken = Guid.NewGuid(), UpdatedAtUtc = clock.UtcNow };
                await tx.WriteMediaAsync(new(state, clock.UtcNow), token);
                return state;
            }, ct);
            if (claim == null) continue;
            try {
                if (claim.ProviderPublicId != null) await media.DeleteAsync(claim.ProviderPublicId, ct);
                await repository.InTransactionAsync(new(claim.OwnerUserId, MediaId: claim.Id), async (tx, token) => {
                    var current = await tx.GetMediaAsync(new(claim.Id), token);
                    if (current?.Status == "Deleting" && current.LeaseToken == claim.LeaseToken) {
                        await tx.WriteMediaAsync(new(current with { Status = "Deleted", IsDeleted = true }, clock.UtcNow), token);
                    }
                    return true;
                }, ct);
                count++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch {
                // Retain provider ID and lease proof. Another worker may retry after the lease expires.
            }
        }
        return count;
    }
}
