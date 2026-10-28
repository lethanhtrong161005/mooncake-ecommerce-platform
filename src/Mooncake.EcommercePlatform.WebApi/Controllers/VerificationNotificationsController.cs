namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using C = Mooncake.EcommercePlatform.Application.Common.Constant.SupplierVerificationConstants;

/// <summary>Feature-scoped, owner-only durable notification inbox and read receipts.</summary>
[Authorize]
[Route("api/v1/notifications")]
[EnableRateLimiting(C.RatePolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VerificationNotificationsController(IVerificationNotificationService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, string category = C.Category, CancellationToken ct = default)
    {
        SupplierVerificationHelper.Require(category == C.Category);
        return Success(await service.InboxAsync(UserId, page, pageSize, ct), C.Success);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> Unread(string category = C.Category, CancellationToken ct = default)
    {
        SupplierVerificationHelper.Require(category == C.Category);
        var inbox = await service.InboxAsync(UserId, 1, 1, ct);
        return Success(new { inbox.UnreadCount, inbox.ReadThroughToken }, C.Success);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        await service.ReadAsync(UserId, id, ct);
        return Success(C.Success);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll([FromBody] ReadVerificationNotificationsRequest request, CancellationToken ct)
    {
        SupplierVerificationHelper.Require(request.Category == C.Category);
        await service.ReadAllAsync(UserId, request.ReadThroughToken, Request.Headers["Idempotency-Key"].ToString(), ct);
        return Success(C.Success);
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReadVerificationNotificationsRequest(
    [Required] string ReadThroughToken, string Category = C.Category);
