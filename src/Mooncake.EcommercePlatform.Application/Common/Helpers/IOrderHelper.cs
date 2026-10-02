namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for mapping Order entities to DTOs.</summary>
public interface IOrderHelper
{
    OrderResponse ToResponse(Order order);
    OrderItemResponse ToItemResponse(OrderItem item);
}
