namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class DeliveryService(IDeliveryRepository repository, IDateTimeProvider clock) : IDeliveryService
{
    public async Task<IReadOnlyList<DeliveryResponse>> GetMineAsync(Guid userId, bool supplier,
        CancellationToken cancellationToken = default)
    {
        var deliveries = await repository.GetMineAsync(userId, supplier, cancellationToken);
        var responses = new List<DeliveryResponse>(deliveries.Count);
        foreach (var delivery in deliveries)
            responses.Add(await ToResponseAsync(delivery, cancellationToken));
        return responses;
    }

    public async Task<DeliveryResponse> GetAsync(Guid userId, bool supplier, Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        var delivery = await GetOwnedAsync(userId, supplier, deliveryId, cancellationToken);
        return await ToResponseAsync(delivery, cancellationToken);
    }

    public async Task<DeliveryResponse> CreateAsync(Guid supplierUserId, CreateDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrderId.HasValue == request.ContractId.HasValue)
            throw new HttpException(400, "Provide exactly one order ID or contract ID.");
        if (!Enum.IsDefined(request.DeliveryType)) throw new HttpException(400, "Delivery type is invalid.");
        var now = clock.UtcNow;
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(), OrderId = request.OrderId, ContractId = request.ContractId,
            DeliveryNumber = $"DLV-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(),
            DeliveryType = request.DeliveryType, ScheduledDate = request.ScheduledDate,
            FromAddress = request.FromAddress?.Trim(), ToAddress = request.ToAddress?.Trim(),
            RecipientName = request.RecipientName?.Trim(), RecipientPhone = request.RecipientPhone?.Trim(),
            Notes = request.Notes?.Trim(), Status = DeliveryStatus.Pending,
            CreatedAt = now, UpdatedAt = now, CreatedBy = supplierUserId
        };
        if (!await repository.CreateAsync(delivery, supplierUserId, cancellationToken))
            throw new HttpException(404, "A supplier-owned order or active contract was not found.");
        return await ToResponseAsync(delivery, cancellationToken);
    }

    public async Task<DeliveryResponse> TransitionAsync(Guid userId, bool supplier, Guid deliveryId,
        DeliveryStatus status, string? trackingCode, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAsync(userId, supplier, deliveryId, cancellationToken);
        if (!Enum.IsDefined(status)) throw new HttpException(400, "Delivery status is invalid.");
        if (trackingCode?.Length > 100) throw new HttpException(400, "Tracking code is too long.");
        if (!await repository.TransitionAsync(deliveryId, userId, supplier, status, trackingCode?.Trim(), cancellationToken))
            throw new HttpException(409, "The delivery status transition is not allowed.");
        return await GetAsync(userId, supplier, deliveryId, cancellationToken);
    }

    public async Task<DeliveryResponse> AddProofAsync(Guid userId, bool supplier, Guid deliveryId,
        AddDeliveryProofRequest request, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAsync(userId, supplier, deliveryId, cancellationToken);
        if (!Enum.IsDefined(request.ProofType)) throw new HttpException(400, "Proof type is invalid.");
        if (request.ProofType == ProofType.Delivery && string.IsNullOrWhiteSpace(request.SignatureUrl))
            throw new HttpException(400, "A recipient signature file is required for delivery proof.");
        var proof = new DeliveryProof
        {
            Id = Guid.NewGuid(), DeliveryId = deliveryId, ProofType = request.ProofType,
            PhotoUrl = request.PhotoUrl.Trim(), SignatureUrl = request.SignatureUrl?.Trim(),
            LocationLat = request.LocationLat, LocationLng = request.LocationLng, Notes = request.Notes?.Trim(),
            TakenBy = userId.ToString(), TakenAt = clock.UtcNow, CreatedBy = userId
        };
        if (!await repository.AddProofAsync(deliveryId, userId, supplier, proof, cancellationToken))
            throw new HttpException(404, "Delivery was not found.");
        return await GetAsync(userId, supplier, deliveryId, cancellationToken);
    }

    private async Task<Delivery> GetOwnedAsync(Guid userId, bool supplier, Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var delivery = await repository.GetAsync(deliveryId, cancellationToken);
        if (delivery is null || !await repository.CanAccessAsync(delivery, userId, supplier, cancellationToken))
            throw new HttpException(404, "Delivery was not found.");
        return delivery;
    }

    private async Task<DeliveryResponse> ToResponseAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        var proofs = await repository.GetProofsAsync(delivery.Id, cancellationToken);
        return new DeliveryResponse(delivery.Id, delivery.OrderId, delivery.ContractId, delivery.DeliveryNumber,
            delivery.DeliveryType, delivery.ScheduledDate, delivery.ActualDeliveryDate, delivery.FromAddress,
            delivery.ToAddress, delivery.RecipientName, delivery.RecipientPhone, delivery.TrackingCode,
            delivery.Status, delivery.Notes, proofs.Select(proof => new DeliveryProofResponse(proof.Id,
                proof.ProofType, proof.PhotoUrl, proof.SignatureUrl, proof.TakenBy, proof.TakenAt,
                proof.LocationLat, proof.LocationLng, proof.Notes)).ToArray());
    }
}
