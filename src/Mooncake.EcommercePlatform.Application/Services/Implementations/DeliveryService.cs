namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements delivery shipments, status updates, GPS proof attachments, and on-time reputation awards.</summary>
public class DeliveryService(
    IDeliveryRepository deliveryRepository,
    IOrderRepository orderRepository,
    IContractRepository contractRepository,
    IReputationLogRepository reputationLogRepository,
    ISupplierRepository supplierRepository,
    IDeliveryHelper deliveryHelper) : IDeliveryService
{
    public async Task<DeliveryResponse> CreateDeliveryAsync(CreateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OrderId.HasValue && request.ContractId.HasValue)
        {
            throw new HttpException(400, "Delivery must target either an Order OR a Contract, not both.");
        }

        if (!request.OrderId.HasValue && !request.ContractId.HasValue)
        {
            throw new HttpException(400, "Delivery must target either an Order or a Contract.");
        }

        var now = DateTime.UtcNow;

        var delivery = new Delivery
        {
            OrderId = request.OrderId,
            ContractId = request.ContractId,
            Direction = request.Direction,
            Status = DeliveryStatus.Pending,
            CarrierName = request.CarrierName?.Trim(),
            TrackingCode = request.TrackingCode?.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            ScheduledAtUtc = request.ScheduledAtUtc,
            Note = request.Note?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var created = await deliveryRepository.CreateAsync(delivery, cancellationToken);
        var loaded = await deliveryRepository.GetByIdAsync(created.Id, cancellationToken);

        return deliveryHelper.ToResponse(loaded ?? created);
    }

    public async Task<DeliveryResponse> UpdateStatusAsync(long id, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await deliveryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Delivery with ID {id} was not found.");

        var now = DateTime.UtcNow;
        delivery.Status = request.Status;
        delivery.UpdatedAtUtc = now;

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            delivery.Note = request.Note.Trim();
        }

        if (request.Status == DeliveryStatus.InTransit)
        {
            delivery.ShippedAtUtc ??= now;
        }
        else if (request.Status == DeliveryStatus.Delivered)
        {
            delivery.DeliveredAtUtc ??= now;

            // 1. If associated with an Order, mark the order as Delivered
            if (delivery.OrderId.HasValue)
            {
                var order = await orderRepository.GetByIdAsync(delivery.OrderId.Value, cancellationToken);
                if (order != null && order.Status != OrderStatus.Cancelled)
                {
                    order.Status = OrderStatus.Delivered;
                    order.UpdatedAtUtc = now;
                    await orderRepository.UpdateOrderAsync(order, cancellationToken);
                }
            }

            // 2. If associated with a Contract, check delivery deadline and adjust supplier reputation
            if (delivery.ContractId.HasValue)
            {
                var contract = await contractRepository.GetByIdAsync(delivery.ContractId.Value, cancellationToken);
                if (contract != null)
                {
                    var supplier = await supplierRepository.GetByIdAsync(contract.SupplierId, cancellationToken);
                    if (supplier != null)
                    {
                        var deliveredDate = DateOnly.FromDateTime(delivery.DeliveredAtUtc.Value);
                        bool isOnTime = deliveredDate <= contract.DeliveryDeadline;

                        int scoreDelta = isOnTime ? 15 : -15;
                        var eventType = isOnTime ? ReputationEventType.ContractOnTime : ReputationEventType.ContractLate;
                        var reason = isOnTime
                            ? $"Contract #{contract.Id} fulfilled on time ({deliveredDate:yyyy-MM-dd} <= {contract.DeliveryDeadline:yyyy-MM-dd})"
                            : $"Contract #{contract.Id} delivered late ({deliveredDate:yyyy-MM-dd} > {contract.DeliveryDeadline:yyyy-MM-dd})";

                        var log = new ReputationLog
                        {
                            SupplierId = supplier.Id,
                            EventType = eventType,
                            ScoreDelta = scoreDelta,
                            ContractId = contract.Id,
                            Reason = reason,
                            CreatedAtUtc = now
                        };

                        await reputationLogRepository.CreateAsync(log, cancellationToken);

                        supplier.ReputationScore = Math.Clamp(supplier.ReputationScore + scoreDelta, 0, 1000);
                        supplier.UpdatedAtUtc = now;
                        await supplierRepository.UpdateAsync(supplier, cancellationToken);
                    }
                }
            }
        }

        await deliveryRepository.UpdateAsync(delivery, cancellationToken);
        var loaded = await deliveryRepository.GetByIdAsync(id, cancellationToken);

        return deliveryHelper.ToResponse(loaded ?? delivery);
    }

    public async Task<DeliveryProofResponse> AddProofAsync(long id, AddDeliveryProofRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await deliveryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new HttpException(404, $"Delivery with ID {id} was not found.");

        var proof = new DeliveryProof
        {
            DeliveryId = delivery.Id,
            ProofType = request.ProofType,
            PhotoUrl = request.PhotoUrl.Trim(),
            TakenByUserId = request.TakenByUserId,
            TakenAtUtc = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Note = request.Note?.Trim()
        };

        var created = await deliveryRepository.AddProofAsync(proof, cancellationToken);
        return deliveryHelper.ToProofResponse(created);
    }

    public async Task<IEnumerable<DeliveryResponse>> GetDeliveriesAsync(
        long? orderId = null,
        long? contractId = null,
        DeliveryStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var deliveries = await deliveryRepository.GetDeliveriesAsync(orderId, contractId, status, cancellationToken);
        return deliveries.Select(deliveryHelper.ToResponse);
    }

    public async Task<DeliveryResponse?> GetDeliveryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var delivery = await deliveryRepository.GetByIdAsync(id, cancellationToken);
        return delivery == null ? null : deliveryHelper.ToResponse(delivery);
    }
}
