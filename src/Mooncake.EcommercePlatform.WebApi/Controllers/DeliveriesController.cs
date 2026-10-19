namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;

[Authorize(Roles = "Customer,Supplier")]
[Route("api/v1/deliveries")]
public sealed class DeliveriesController(IDeliveryService deliveryService) : BaseApiController
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await deliveryService.GetMineAsync(CurrentUserId, User.IsInRole("Supplier"), cancellationToken),
            "Deliveries retrieved successfully.");

    [HttpGet("{deliveryId:guid}")]
    public async Task<IActionResult> GetAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetAsync(CurrentUserId, User.IsInRole("Supplier"), deliveryId, cancellationToken),
            "Delivery retrieved successfully.");

    [Authorize(Roles = "Supplier")]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Created(await deliveryService.CreateAsync(CurrentUserId, request, cancellationToken),
            "Delivery created successfully.");

    [HttpPost("{deliveryId:guid}/status")]
    public async Task<IActionResult> UpdateStatusAsync(Guid deliveryId, [FromBody] UpdateDeliveryStatusRequest request,
        CancellationToken cancellationToken) =>
        Success(await deliveryService.TransitionAsync(CurrentUserId, User.IsInRole("Supplier"), deliveryId,
            request.Status, request.TrackingCode, cancellationToken), "Delivery status updated successfully.");

    [HttpPost("{deliveryId:guid}/proofs")]
    public async Task<IActionResult> AddProofAsync(Guid deliveryId, [FromBody] AddDeliveryProofRequest request,
        CancellationToken cancellationToken) =>
        Success(await deliveryService.AddProofAsync(CurrentUserId, User.IsInRole("Supplier"), deliveryId,
            request, cancellationToken), "Delivery proof added successfully.");

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("A valid user identity is required.");
}

public sealed record UpdateDeliveryStatusRequest(DeliveryStatus Status, string? TrackingCode);
