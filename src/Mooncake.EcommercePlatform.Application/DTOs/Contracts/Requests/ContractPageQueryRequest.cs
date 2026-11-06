namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ContractPageQueryRequest : PagedQuery
{
    public ContractStatus? Status { get; init; }
}
