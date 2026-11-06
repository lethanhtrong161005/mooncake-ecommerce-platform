namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public sealed record OrderLedgerPageQueryRequest : PagedQuery
{
    public string? Bucket { get; init; }
    public string? EntryType { get; init; }
}
