namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public sealed record ContractDisputePageQueryRequest : PagedQuery
{
    public string? Status { get; init; }
}
