namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

/// <summary>Creates a stable hash for semantically identical order requests.</summary>
public static class IdempotencyRequestHelper
{
    public static string CreateHash(CreateOrderRequest request)
    {
        var canonicalRequest = new
        {
            request.ShopId,
            Items = request.Items.OrderBy(item => item.ProductId).ThenBy(item => item.VariantId)
                .Select(item => new { item.ProductId, item.VariantId, item.Quantity }).ToArray(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            Notes = request.Notes?.Trim(),
            request.DeliveryDateExpected
        };
        var serialized = JsonSerializer.Serialize(canonicalRequest);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
    }
}
