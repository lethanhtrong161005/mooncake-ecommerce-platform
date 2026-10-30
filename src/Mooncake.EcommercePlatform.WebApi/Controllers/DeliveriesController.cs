namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

[Route("api/v1/deliveries")]
[Authorize]
public sealed class DeliveriesController(IDeliveryService deliveryService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken) =>
        Success(await deliveryService.GetMineAsync(
            CurrentUserId,
            User.IsInRole("Supplier") || User.HasClaim("supplier_fulfillment", "true"),
            cancellationToken), "Danh sách chuyến giao hàng được lấy thành công.");

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetAsync(CurrentUserId, id, cancellationToken), "Lấy thông tin chuyến giao hàng thành công.");

    [HttpGet("contract/{contractId:guid}")]
    public async Task<IActionResult> GetByContractAsync(Guid contractId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetByContractAsync(CurrentUserId, contractId, cancellationToken), "Lấy danh sách chuyến giao của hợp đồng thành công.");

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetByOrderAsync(CurrentUserId, orderId, cancellationToken), "Lấy danh sách chuyến giao của đơn hàng thành công.");

    [HttpPost]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.CreateAsync(CurrentUserId, request, cancellationToken);
        return Created(result, "Chuyến giao hàng được tạo thành công.");
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatusAsync(Guid id, [FromBody] UpdateDeliveryStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.UpdateStatusAsync(CurrentUserId, id, request, cancellationToken);
        return Success(result, "Cập nhật trạng thái chuyến giao hàng thành công.");
    }

    [HttpPut("{id:guid}/carrier-info")]
    [Authorize(Roles = "Supplier,Admin")]
    public async Task<IActionResult> UpdateCarrierInfoAsync(Guid id, [FromBody] UpdateCarrierInfoRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.UpdateCarrierInfoAsync(CurrentUserId, id, request, cancellationToken);
        return Success(result, "Cập nhật thông tin vận chuyển thành công.");
    }

    [HttpPost("{id:guid}/proofs")]
    public async Task<IActionResult> SubmitProofAsync(Guid id, [FromBody] CreateDeliveryProofRequest request, CancellationToken cancellationToken)
    {
        var result = await deliveryService.SubmitProofAsync(CurrentUserId, id, request, cancellationToken);
        return Created(result, "Nộp bằng chứng giao nhận (PoD) thành công.");
    }

    [HttpGet("{id:guid}/proofs")]
    public async Task<IActionResult> GetProofsAsync(Guid id, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetProofsAsync(CurrentUserId, id, cancellationToken), "Lấy danh sách bằng chứng giao nhận thành công.");

    [HttpGet("proofs/{proofId:guid}")]
    public async Task<IActionResult> GetProofDetailAsync(Guid proofId, CancellationToken cancellationToken) =>
        Success(await deliveryService.GetProofDetailAsync(CurrentUserId, proofId, cancellationToken), "Lấy thông tin bằng chứng PoD thành công.");

    [HttpGet("proofs/{proofId:guid}/verify")]
    public async Task<IActionResult> VerifyProofIntegrityAsync(Guid proofId, CancellationToken cancellationToken)
    {
        var isVerified = await deliveryService.VerifyProofIntegrityAsync(proofId, cancellationToken);
        return Success(new { isVerified }, "Xác minh tính toàn vẹn của bằng chứng PoD thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("Yêu cầu phiên làm việc hợp lệ.");
}
