namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Supplier custom packaging template management and customer storefront endpoints.</summary>
[ApiController]
public sealed class PackagingTemplatesController(IPackagingTemplateService service) : BaseApiController
{
    /// <summary>Returns the active packaging templates for a given shop (Customer / Public).</summary>
    [HttpGet("/api/v1/shops/{shopId:guid}/packaging-templates")]
    public async Task<IActionResult> GetByShopIdAsync(Guid shopId, CancellationToken ct) =>
        Success(await service.GetByShopIdAsync(shopId, ct), "Tải danh sách mẫu bao bì thành công.");

    /// <summary>Returns the current supplier's packaging templates.</summary>
    [Authorize]
    [HttpGet("/api/v1/suppliers/me/packaging-templates")]
    public async Task<IActionResult> GetMineAsync(CancellationToken ct) =>
        Success(await service.GetMineAsync(GetCurrentUserId(), ct), "Tải danh sách mẫu bao bì thành công.");

    /// <summary>Creates a new packaging template for the supplier's shop.</summary>
    [Authorize]
    [HttpPost("/api/v1/suppliers/me/packaging-templates")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePackagingTemplateRequest request, CancellationToken ct) =>
        Created(await service.CreateAsync(GetCurrentUserId(), request, ct), "Đã thêm mẫu bao bì mới thành công.");

    /// <summary>Updates an existing packaging template.</summary>
    [Authorize]
    [HttpPut("/api/v1/suppliers/me/packaging-templates/{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdatePackagingTemplateRequest request, CancellationToken ct) =>
        Success(await service.UpdateAsync(GetCurrentUserId(), id, request, ct), "Cập nhật mẫu bao bì thành công.");

    /// <summary>Deletes a packaging template.</summary>
    [Authorize]
    [HttpDelete("/api/v1/suppliers/me/packaging-templates/{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(GetCurrentUserId(), id, ct);
        return Success("Đã xóa mẫu bao bì thành công.");
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

