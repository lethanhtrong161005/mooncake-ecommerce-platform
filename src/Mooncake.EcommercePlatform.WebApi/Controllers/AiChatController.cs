namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Controller exposing AI Chatbot Concierge and RAG consultation endpoints.</summary>
[Route("api/v1/ai/chat")]
public sealed class AiChatController(IAiChatService aiChatService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSessionAsync([FromBody] StartAiSessionRequest request, CancellationToken ct)
    {
        var result = await aiChatService.StartSessionAsync(OptionalUserId, request, ct);
        return Success(result, "Khởi tạo phiên hội thoại AI thành công.");
    }

    [AllowAnonymous]
    [HttpPost("messages")]
    public async Task<IActionResult> ProcessMessageAsync([FromBody] SendAiMessageRequest request, CancellationToken ct)
    {
        var result = await aiChatService.ProcessMessageAsync(OptionalUserId, request, ct);
        return Success(result, "Nhận phản hồi tư vấn AI thành công.");
    }

    [AllowAnonymous]
    [HttpGet("sessions/{sessionToken}/history")]
    public async Task<IActionResult> GetHistoryAsync(string sessionToken, CancellationToken ct)
    {
        var result = await aiChatService.GetHistoryAsync(sessionToken, ct);
        return Success(result, "Lấy lịch sử hội thoại AI thành công.");
    }

    [AllowAnonymous]
    [HttpDelete("sessions/{sessionToken}")]
    public async Task<IActionResult> EndSessionAsync(string sessionToken, CancellationToken ct)
    {
        await aiChatService.EndSessionAsync(sessionToken, ct);
        return Success("Kết thúc phiên hội thoại AI thành công.");
    }

    private Guid? OptionalUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : null;
}

