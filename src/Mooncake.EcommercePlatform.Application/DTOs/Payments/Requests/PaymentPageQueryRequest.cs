namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record PaymentPageQueryRequest : PagedQuery
{
    public PaymentStatus? Status { get; init; }
    public PaymentMethod? Method { get; init; }
    public Guid? OrderId { get; init; }
}
