namespace Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public sealed record PromotionPageQueryRequest : PagedQuery
{
    public bool? IsActive { get; init; }
    public Guid? ProductId { get; init; }
}
