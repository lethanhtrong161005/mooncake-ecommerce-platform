namespace Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record DeliveryPageQueryRequest : PagedQuery
{
    public DeliveryStatus? Status { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? ContractId { get; init; }
    public DateOnly? ScheduledFrom { get; init; }
    public DateOnly? ScheduledTo { get; init; }
}
