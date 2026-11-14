namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements shipment scheduling, status tracking, and Proof-of-Delivery verification.</summary>
public sealed class DeliveryService(
    IDeliveryRepository repository,
    IDateTimeProvider clock) : IDeliveryService
{
    public async Task<PagedResult<DeliveryResponse>> GetMinePageAsync(Guid userId, bool isSupplier,
        DeliveryPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "scheduledDateAsc", "scheduledDateDesc");
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value)
            || query.ScheduledFrom > query.ScheduledTo)
            throw new HttpException(400, "Invalid delivery filter.", OrderErrorCodes.ValidationError);
        var page = await repository.GetDeliveriesPageAsync(userId, isSupplier, query, cancellationToken);
        var items = await MapDeliveryListAsync(page.Items, cancellationToken);
        return new PagedResult<DeliveryResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static readonly Regex PhoneRegex = new(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<DeliveryResponse>> GetMineAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default)
    {
        var deliveries = await repository.GetDeliveriesForUserAsync(userId, isSupplier, cancellationToken);
        return await MapDeliveryListAsync(deliveries, cancellationToken);
    }

    public async Task<DeliveryDetailResponse> GetAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.DeliveryNotFound);

        var canAccess = await repository.CanAccessDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, DeliveryMessages.AccessDenied, OrderErrorCodes.TenantForbidden);

        var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
        return DeliveryResponseHelper.ToDetailResponse(delivery, proofs.Select(DeliveryResponseHelper.ToProofResponse).ToArray());
    }

    public async Task<IReadOnlyList<DeliveryResponse>> GetByContractAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        if (!await repository.CanAccessContractAsync(contractId, userId, cancellationToken))
            throw new HttpException(403, DeliveryMessages.AccessDenied, OrderErrorCodes.TenantForbidden);

        var deliveries = await repository.GetByContractIdAsync(contractId, cancellationToken);
        return await MapDeliveryListAsync(deliveries, cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryResponse>> GetByOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!await repository.CanAccessOrderAsync(orderId, userId, cancellationToken))
            throw new HttpException(403, DeliveryMessages.AccessDenied, OrderErrorCodes.TenantForbidden);

        var deliveries = await repository.GetByOrderIdAsync(orderId, cancellationToken);
        return await MapDeliveryListAsync(deliveries, cancellationToken);
    }

    public async Task<DeliveryResponse> CreateAsync(Guid userId, CreateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if ((!request.ContractId.HasValue && !request.OrderId.HasValue) ||
            (request.ContractId.HasValue && request.OrderId.HasValue))
        {
            throw new HttpException(400, DeliveryMessages.TargetLinkInvalid, OrderErrorCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(request.FromAddress))
            throw new HttpException(400, DeliveryMessages.FromAddressRequired, OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.ToAddress))
            throw new HttpException(400, DeliveryMessages.ToAddressRequired, OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.RecipientName))
            throw new HttpException(400, DeliveryMessages.RecipientNameRequired, OrderErrorCodes.ValidationError);

        if (string.IsNullOrWhiteSpace(request.RecipientPhone) || !PhoneRegex.IsMatch(request.RecipientPhone.Trim()))
            throw new HttpException(400, DeliveryMessages.RecipientPhoneInvalid, OrderErrorCodes.ValidationError);

        if (!request.ScheduledDate.HasValue)
            throw new HttpException(400, DeliveryMessages.ScheduledDateRequired, OrderErrorCodes.ValidationError);
        // Delivery dates are calendar dates in the Vietnam marketplace timezone (UTC+7).
        var marketplaceNow = clock.UtcNow.AddHours(7);
        var earliestDelivery = marketplaceNow.AddHours(24);
        var minimumScheduledDate = DateOnly.FromDateTime(earliestDelivery);
        if (minimumScheduledDate.ToDateTime(TimeOnly.MinValue) < earliestDelivery)
            minimumScheduledDate = minimumScheduledDate.AddDays(1);
        if (request.ScheduledDate.Value < minimumScheduledDate)
            throw new HttpException(400, DeliveryMessages.ScheduledDateTooSoon, OrderErrorCodes.ValidationError);

        if (request.ContractId.HasValue)
        {
            if (!await repository.CanManageContractAsync(request.ContractId.Value, userId, cancellationToken))
                throw new HttpException(403, DeliveryMessages.ManageDenied, OrderErrorCodes.TenantForbidden);
        }
        else if (request.OrderId.HasValue)
        {
            if (!await repository.CanManageOrderAsync(request.OrderId.Value, userId, cancellationToken))
                throw new HttpException(403, DeliveryMessages.ManageDenied, OrderErrorCodes.TenantForbidden);

            var existingOrderDeliveries = await repository.GetByOrderIdAsync(request.OrderId.Value, cancellationToken) ?? [];
            if (existingOrderDeliveries.Any(d => d.Status is not (DeliveryStatus.Failed or DeliveryStatus.Returned)))
                throw new HttpException(409, DeliveryMessages.SingleDeliveryLimitExceeded, OrderErrorCodes.SingleDeliveryLimit);
        }

        var deliveryNumber = $"DEL-{clock.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}";
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            ContractId = request.ContractId,
            OrderId = request.OrderId,
            DeliveryNumber = deliveryNumber,
            DeliveryType = request.DeliveryType,
            Status = DeliveryStatus.Pending,
            ScheduledDate = request.ScheduledDate,
            FromAddress = request.FromAddress.Trim(),
            ToAddress = request.ToAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedBy = userId,
            CreatedAt = clock.UtcNow
        };

        var created = await repository.CreateAsync(delivery, cancellationToken);
        return DeliveryResponseHelper.ToResponse(created, 0);
    }

    public async Task<DeliveryResponse> UpdateStatusAsync(Guid userId, Guid deliveryId, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.DeliveryNotFound);

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, DeliveryMessages.ManageDenied, OrderErrorCodes.TenantForbidden);

        if (delivery.OrderId.HasValue && request.Status == DeliveryStatus.Delivered)
            throw new HttpException(409, "Direct order deliveries can only be marked delivered after customer receipt confirmation.", OrderErrorCodes.StateConflict);

        ValidateStateTransition(delivery.Status, request.Status);

        if (request.Status == DeliveryStatus.Delivered)
        {
            var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
            if (!proofs.Any(p => p.ProofType == ProofType.Delivery))
                throw new HttpException(400, DeliveryMessages.ProofRequiredBeforeDelivered, OrderErrorCodes.DeliveryProofRequired);
            delivery.ActualDeliveryDate = request.ActualDeliveryDate ?? DateOnly.FromDateTime(clock.UtcNow);
        }

        delivery.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            delivery.Notes = string.IsNullOrWhiteSpace(delivery.Notes)
                ? request.Notes.Trim()
                : $"{delivery.Notes}\n[{clock.UtcNow:yyyy-MM-dd HH:mm}] {request.Notes.Trim()}";
        }
        delivery.UpdatedBy = userId;
        delivery.UpdatedAt = clock.UtcNow;

        var updated = await repository.UpdateAsync(delivery, cancellationToken);
        var proofCount = (await repository.GetProofsAsync(deliveryId, cancellationToken)).Count;
        return DeliveryResponseHelper.ToResponse(updated, proofCount);
    }

    public async Task<DeliveryResponse> UpdateCarrierInfoAsync(Guid userId, Guid deliveryId, UpdateCarrierInfoRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.DeliveryNotFound);

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, DeliveryMessages.ManageDenied, OrderErrorCodes.TenantForbidden);

        if (!string.IsNullOrWhiteSpace(request.TrackingCode))
            delivery.TrackingCode = request.TrackingCode.Trim();

        if (!string.IsNullOrWhiteSpace(request.RecipientName))
            delivery.RecipientName = request.RecipientName.Trim();

        if (!string.IsNullOrWhiteSpace(request.RecipientPhone))
        {
            if (!PhoneRegex.IsMatch(request.RecipientPhone.Trim()))
                throw new HttpException(400, DeliveryMessages.RecipientPhoneInvalid, OrderErrorCodes.ValidationError);
            delivery.RecipientPhone = request.RecipientPhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.FromAddress))
            delivery.FromAddress = request.FromAddress.Trim();

        if (!string.IsNullOrWhiteSpace(request.ToAddress))
            delivery.ToAddress = request.ToAddress.Trim();

        if (!string.IsNullOrWhiteSpace(request.Notes))
            delivery.Notes = request.Notes.Trim();

        delivery.UpdatedBy = userId;
        delivery.UpdatedAt = clock.UtcNow;

        var updated = await repository.UpdateAsync(delivery, cancellationToken);
        var proofCount = (await repository.GetProofsAsync(deliveryId, cancellationToken)).Count;
        return DeliveryResponseHelper.ToResponse(updated, proofCount);
    }

    public async Task<DeliveryProofResponse> SubmitProofAsync(Guid userId, Guid deliveryId, CreateDeliveryProofRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.DeliveryNotFound);

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, DeliveryMessages.ProofSubmitDenied, OrderErrorCodes.TenantForbidden);

        if (delivery.Status == DeliveryStatus.Delivered)
            throw new HttpException(409, DeliveryMessages.DeliveryAlreadyCompleted, OrderErrorCodes.StateConflict);

        if (request.LocationLat.HasValue && (request.LocationLat.Value < -90m || request.LocationLat.Value > 90m))
            throw new HttpException(400, DeliveryMessages.LatitudeInvalid, OrderErrorCodes.ValidationError);

        if (request.LocationLng.HasValue && (request.LocationLng.Value < -180m || request.LocationLng.Value > 180m))
            throw new HttpException(400, DeliveryMessages.LongitudeInvalid, OrderErrorCodes.ValidationError);

        var photoUrl = request.PhotoUrl ?? request.PhotoBase64;
        var signatureUrl = request.SignatureUrl ?? request.SignatureBase64;

        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new HttpException(400, DeliveryMessages.PhotoRequired, OrderErrorCodes.ValidationError);

        if (delivery.ContractId.HasValue && request.ProofType == ProofType.Delivery && string.IsNullOrWhiteSpace(signatureUrl))
            throw new HttpException(400, DeliveryMessages.SignatureRequiredForContract, OrderErrorCodes.ValidationError);

        var rawPayload = $"{deliveryId}|{request.ProofType}|{request.LocationLat}|{request.LocationLng}|{clock.UtcNow:O}|{photoUrl}|{signatureUrl}";
        var tamperProofHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();

        var proof = new DeliveryProof
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            ProofType = request.ProofType,
            PhotoUrl = photoUrl,
            SignatureUrl = signatureUrl,
            TakenBy = string.IsNullOrWhiteSpace(request.TakenBy) ? DeliveryMessages.DefaultDriverLabel : request.TakenBy.Trim(),
            TakenAt = clock.UtcNow,
            LocationLat = request.LocationLat,
            LocationLng = request.LocationLng,
            Notes = request.Notes?.Trim(),
            TamperProofHash = tamperProofHash,
            CreatedBy = userId,
            CreatedAt = clock.UtcNow
        };

        var createdProof = await repository.CreateProofAsync(proof, cancellationToken);

        if (request.ProofType == ProofType.Delivery)
        {
            if (delivery.OrderId.HasValue)
            {
                delivery.Status = DeliveryStatus.AwaitingCustomerConfirmation;
                delivery.UpdatedBy = userId;
                delivery.UpdatedAt = clock.UtcNow;
                await repository.UpdateAsync(delivery, cancellationToken);
            }
            else
            {
                delivery.Status = DeliveryStatus.Delivered;
                delivery.ActualDeliveryDate = DateOnly.FromDateTime(clock.UtcNow);
                delivery.UpdatedBy = userId;
                delivery.UpdatedAt = clock.UtcNow;
                await repository.UpdateAsync(delivery, cancellationToken);
            }
        }
        else if (request.ProofType == ProofType.Pickup && (delivery.Status == DeliveryStatus.Pending || delivery.Status == DeliveryStatus.Confirmed))
        {
            delivery.Status = DeliveryStatus.PickedUp;
            delivery.UpdatedBy = userId;
            delivery.UpdatedAt = clock.UtcNow;
            await repository.UpdateAsync(delivery, cancellationToken);
        }
        else if (request.ProofType == ProofType.FailedAttempt)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.UpdatedBy = userId;
            delivery.UpdatedAt = clock.UtcNow;
            await repository.UpdateAsync(delivery, cancellationToken);
        }

        return DeliveryResponseHelper.ToProofResponse(createdProof);
    }

    public async Task<IReadOnlyList<DeliveryProofResponse>> GetProofsAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var canAccess = await repository.CanAccessDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, DeliveryMessages.AccessDenied, OrderErrorCodes.TenantForbidden);

        var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
        return proofs.Select(DeliveryResponseHelper.ToProofResponse).ToArray();
    }

    public async Task<DeliveryProofResponse> GetProofDetailAsync(Guid userId, Guid proofId, CancellationToken cancellationToken = default)
    {
        var proof = await repository.GetProofByIdAsync(proofId, cancellationToken)
            ?? throw new HttpException(404, DeliveryMessages.ProofNotFound);

        var canAccess = await repository.CanAccessDeliveryAsync(proof.DeliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, DeliveryMessages.AccessDenied, OrderErrorCodes.TenantForbidden);

        return DeliveryResponseHelper.ToProofResponse(proof);
    }

    public async Task<bool> VerifyProofIntegrityAsync(Guid proofId, CancellationToken cancellationToken = default)
    {
        var proof = await repository.GetProofByIdAsync(proofId, cancellationToken);
        return proof is not null && !string.IsNullOrWhiteSpace(proof.TamperProofHash) && proof.TamperProofHash.Length == 64;
    }

    private async Task<IReadOnlyList<DeliveryResponse>> MapDeliveryListAsync(IReadOnlyList<Delivery> deliveries, CancellationToken cancellationToken)
    {
        if (deliveries.Count == 0)
            return [];

        var counts = await repository.GetProofCountsAsync(deliveries.Select(d => d.Id).ToArray(), cancellationToken);
        var responses = new List<DeliveryResponse>(deliveries.Count);
        foreach (var delivery in deliveries)
        {
            var proofCount = counts is not null
                ? counts.GetValueOrDefault(delivery.Id, 0)
                : (await repository.GetProofsAsync(delivery.Id, cancellationToken)).Count;
            responses.Add(DeliveryResponseHelper.ToResponse(delivery, proofCount));
        }

        return responses;
    }

    private static void ValidateStateTransition(DeliveryStatus current, DeliveryStatus next)
    {
        if (current == next) return;

        var isValid = current switch
        {
            DeliveryStatus.Pending => next is DeliveryStatus.Confirmed or DeliveryStatus.PickedUp or DeliveryStatus.Failed,
            DeliveryStatus.Confirmed => next is DeliveryStatus.PickedUp or DeliveryStatus.InTransit or DeliveryStatus.Failed,
            DeliveryStatus.PickedUp => next is DeliveryStatus.InTransit or DeliveryStatus.Failed,
            DeliveryStatus.InTransit => next is DeliveryStatus.AwaitingCustomerConfirmation or DeliveryStatus.Delivered or DeliveryStatus.Failed or DeliveryStatus.Returned,
            DeliveryStatus.AwaitingCustomerConfirmation => next is DeliveryStatus.Delivered or DeliveryStatus.Disputed or DeliveryStatus.Failed or DeliveryStatus.Returned,
            DeliveryStatus.Disputed => next is DeliveryStatus.Delivered or DeliveryStatus.Returned,
            DeliveryStatus.Failed => next is DeliveryStatus.InTransit or DeliveryStatus.Returned,
            DeliveryStatus.Delivered => false,
            DeliveryStatus.Returned => false,
            _ => false
        };

        if (!isValid)
            throw new HttpException(400, $"Cannot transition delivery status from '{current}' to '{next}'.", OrderErrorCodes.StateConflict);
    }
}
