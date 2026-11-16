namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Security.Cryptography;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Drivers.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Helper methods for driver profile mappings and public delivery payload generation.</summary>
public static class DriverHelper
{
    public static DriverProfileResponse ToProfileResponse(DriverProfile profile) =>
        new(profile.UserId, profile.FullName, profile.Phone, profile.DefaultVehiclePlate, profile.CreatedAt);

    public static string GenerateShareToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static async Task<PublicDriverDeliveryResponse> BuildPublicDriverResponseAsync(
        Delivery delivery,
        IDeliveryRepository repository,
        CancellationToken cancellationToken)
    {
        var proofs = await repository.GetProofsAsync(delivery.Id, cancellationToken);
        var referenceCode = await repository.GetReferenceCodeAsync(delivery, cancellationToken);
        var items = ((await repository.GetDeliveryItemsAsync(delivery.Id, cancellationToken)) ?? [])
            .Select(DeliveryResponseHelper.ToItemResponse).ToArray();
        var totalBatches = delivery.ContractId.HasValue
            ? await repository.GetTotalBatchesForContractAsync(delivery.ContractId.Value, cancellationToken)
            : (int?)null;
        var partyContextMap = await repository.GetPartyContextsAsync([delivery], cancellationToken);
        DeliveryPartyContext? partyContext = null;
        partyContextMap?.TryGetValue(delivery.Id, out partyContext);

        return DeliveryResponseHelper.ToPublicDriverResponse(
            delivery,
            proofs.Count,
            referenceCode,
            items,
            totalBatches,
            partyContext);
    }
}
