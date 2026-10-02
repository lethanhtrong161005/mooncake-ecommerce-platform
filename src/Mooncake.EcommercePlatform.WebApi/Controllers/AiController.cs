namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>AI Assistant chat and sales analytics reporting endpoints.</summary>
[Route("api/v1/ai")]
public class AiController(IAiService aiService) : BaseApiController
{
    // ── Conversations ───────────────────────────────────────────────────
    /// <summary>Creates a new AI conversation.</summary>
    [HttpPost("conversations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConversationAsync([FromQuery] long userId, [FromBody] CreateAiConversationRequest request, CancellationToken cancellationToken)
    {
        var conversation = await aiService.CreateConversationAsync(userId, request, cancellationToken);
        return Created(conversation, "AI conversation created successfully.");
    }

    /// <summary>Lists conversations for a user.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationsAsync([FromQuery] long userId, CancellationToken cancellationToken)
    {
        var conversations = await aiService.GetUserConversationsAsync(userId, cancellationToken);
        return Success(conversations, "AI conversations retrieved successfully.");
    }

    /// <summary>Gets a conversation with its messages.</summary>
    [HttpGet("conversations/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationByIdAsync(long id, CancellationToken cancellationToken)
    {
        var conversation = await aiService.GetConversationByIdAsync(id, cancellationToken);
        return Success(conversation, "AI conversation retrieved successfully.");
    }

    /// <summary>Sends a message into a conversation and returns the AI reply.</summary>
    [HttpPost("conversations/{id:long}/messages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessageAsync(long id, [FromBody] SendAiMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await aiService.SendMessageAsync(id, request, cancellationToken);
        return Created(message, "AI response generated successfully.");
    }

    // ── Reports ──────────────────────────────────────────────────────────
    /// <summary>Generates an AI sales analytics report for a supplier.</summary>
    [HttpPost("reports/generate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReportAsync([FromBody] GenerateAiReportRequest request, CancellationToken cancellationToken)
    {
        var report = await aiService.GenerateReportAsync(request, cancellationToken);
        return Created(report, "AI sales analytics report generated successfully.");
    }

    /// <summary>Lists reports generated for a supplier.</summary>
    [HttpGet("reports/supplier/{supplierId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierReportsAsync(long supplierId, CancellationToken cancellationToken)
    {
        var reports = await aiService.GetSupplierReportsAsync(supplierId, cancellationToken);
        return Success(reports, "Supplier AI reports retrieved successfully.");
    }

    /// <summary>Gets an AI analytics report by ID.</summary>
    [HttpGet("reports/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportByIdAsync(long id, CancellationToken cancellationToken)
    {
        var report = await aiService.GetReportByIdAsync(id, cancellationToken);
        return Success(report, "AI report retrieved successfully.");
    }
}
