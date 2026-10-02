namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Two-way shipment management, status transitions, GPS-tagged proofs, and on-time delivery verification.</summary>
[Route("api/v1/deliveries")]
public class DeliveriesController(IDeliveryService deliveryService) : BaseApiController
{
    /// <summary>Initiates a new delivery shipment for an Order or Contract.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDeliveryAsync([FromBody] CreateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.CreateDeliveryAsync(request, cancellationToken);
        return Created(result, "Delivery initiated successfully.");
    }

    /// <summary>Lists delivery shipments with optional order, contract, and status filters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveriesAsync(
        [FromQuery] long? orderId,
        [FromQuery] long? contractId,
        [FromQuery] DeliveryStatus? status,
        CancellationToken cancellationToken)
    {
        var deliveries = await deliveryService.GetDeliveriesAsync(orderId, contractId, status, cancellationToken);
        return Success(deliveries, "Deliveries retrieved successfully.");
    }

    /// <summary>Gets single delivery shipment details including GPS-tagged proofs.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeliveryByIdAsync(long id, CancellationToken cancellationToken)
    {
        var delivery = await deliveryService.GetDeliveryByIdAsync(id, cancellationToken);
        if (delivery == null)
        {
            return NotFound($"Delivery with ID {id} was not found.");
        }

        return Success(delivery, "Delivery details retrieved successfully.");
    }

    /// <summary>Updates delivery lifecycle status (InTransit, Delivered, Failed, Returned) and evaluates on-time penalty/award.</summary>
    [HttpPut("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatusAsync(long id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.UpdateStatusAsync(id, request, cancellationToken);
        return Success(result, "Delivery status updated successfully.");
    }

    /// <summary>Attaches a photo proof with GPS coordinates to the delivery shipment.</summary>
    [HttpPost("{id:long}/proofs")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProofAsync(long id, [FromBody] AddDeliveryProofRequest request, CancellationToken cancellationToken)
    {
        var proof = await deliveryService.AddProofAsync(id, request, cancellationToken);
        return Created(proof, "Delivery proof attached successfully.");
    }
}
