namespace Mooncake.EcommercePlatform.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;

/// <summary>Controller exposing AI Sales Analytics and Executive Market Intelligence endpoints.</summary>
[Route("api/v1/ai/analytics")]
public sealed class AiAnalyticsController(IAiAnalyticsService aiAnalyticsService) : BaseApiController
{
    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Supplier")]
    public async Task<IActionResult> GenerateReportAsync([FromBody] GenerateAiAnalyticsReportRequest request, CancellationToken ct)
    {
        var result = await aiAnalyticsService.GenerateReportAsync(CurrentUserId, IsAdmin, request, ct);
        return Created(result, "Sinh báo cáo phân tích kinh doanh AI thành công.");
    }

    [HttpGet("reports")]
    [Authorize(Roles = "Admin,Supplier")]
    public async Task<IActionResult> ListReportsAsync(CancellationToken ct)
    {
        var result = await aiAnalyticsService.ListReportsAsync(CurrentUserId, IsAdmin, ct);
        return Success(result, "Lấy danh sách báo cáo phân tích AI thành công.");
    }

    [HttpGet("reports/{id:guid}")]
    [Authorize(Roles = "Admin,Supplier")]
    public async Task<IActionResult> GetReportDetailsAsync(Guid id, CancellationToken ct)
    {
        var result = await aiAnalyticsService.GetReportDetailsAsync(CurrentUserId, IsAdmin, id, ct);
        return Success(result, "Lấy chi tiết báo cáo phân tích AI thành công.");
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin,Supplier")]
    public async Task<IActionResult> GetDashboardSummaryAsync([FromQuery] Guid? shopId, CancellationToken ct)
    {
        var result = await aiAnalyticsService.GetDashboardSummaryAsync(shopId, ct);
        return Success(result, "Lấy tổng hợp chỉ số vận hành thời gian thực thành công.");
    }

    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId : throw new UnauthorizedAccessException("Yêu cầu phiên làm việc hợp lệ.");

    private bool IsAdmin => User.IsInRole("Admin");
}

