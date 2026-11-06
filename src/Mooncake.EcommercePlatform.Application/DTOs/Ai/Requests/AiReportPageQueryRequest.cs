namespace Mooncake.EcommercePlatform.Application.DTOs.Ai.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record AiReportPageQueryRequest : PagedQuery
{
    public AiReportType? Type { get; init; }
}
