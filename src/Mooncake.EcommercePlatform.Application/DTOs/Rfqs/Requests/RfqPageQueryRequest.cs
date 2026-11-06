namespace Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record RfqPageQueryRequest : PagedQuery
{
    public RfqStatus? Status { get; init; }
}
