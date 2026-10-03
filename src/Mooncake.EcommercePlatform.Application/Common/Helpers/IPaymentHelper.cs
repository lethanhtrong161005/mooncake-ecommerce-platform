namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Payment mapping helper interface.</summary>
public interface IPaymentHelper
{
    PaymentResponse ToResponse(Payment payment);
}
