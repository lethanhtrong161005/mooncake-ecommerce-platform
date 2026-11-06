namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public sealed record AdminOperationPageQueryRequest : PagedQuery
{
    public string? Status { get; init; }
    public Guid? ShopId { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
}
