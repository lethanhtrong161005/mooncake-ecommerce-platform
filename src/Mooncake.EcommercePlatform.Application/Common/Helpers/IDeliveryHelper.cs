namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Delivery mapping helper interface.</summary>
public interface IDeliveryHelper
{
    DeliveryResponse ToResponse(Delivery delivery);
    DeliveryProofResponse ToProofResponse(DeliveryProof proof);
}
