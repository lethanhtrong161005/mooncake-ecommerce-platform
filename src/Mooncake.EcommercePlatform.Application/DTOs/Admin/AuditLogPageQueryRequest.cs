namespace Mooncake.EcommercePlatform.Application.DTOs.Admin;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public sealed record AuditLogPageQueryRequest : PagedQuery
{
    public string? Action { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? EntityName { get; init; }
}
