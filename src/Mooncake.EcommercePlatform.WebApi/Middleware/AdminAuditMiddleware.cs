namespace Mooncake.EcommercePlatform.WebApi.Middleware;

using System.Security.Claims;
using System.Text.Json;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>Records successful authenticated write operations in the administrative audit trail.</summary>
public sealed class AdminAuditMiddleware(RequestDelegate next, ILogger<AdminAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        await next(context);
        if (!context.User.IsInRole("Admin") || context.Response.StatusCode is < 200 or >= 300 || HttpMethods.IsGet(context.Request.Method) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return;
        if (context.Request.Path.StartsWithSegments("/api/v1/admin/users") || context.Request.Path.StartsWithSegments("/api/v1/admin/system-config")) return;

        try
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actorId,
                Action = "ApiWrite",
                EntityName = "Route",
                EntityId = context.Request.Path.Value,
                MetadataJson = JsonSerializer.Serialize(new { context.Request.Method, context.Response.StatusCode }),
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist API audit record for {Path}", context.Request.Path);
        }
    }
}
