namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Computes deterministic quote version hashes and VNPay capped payment part splits.</summary>
public static class PriceQuoteVersionHelper
{
    public static string ComputeVersion(
        Guid shopId,
        IEnumerable<(Guid VariantId, int Quantity, decimal OriginalUnitPrice, decimal DiscountedUnitPrice, Guid? PromotionId)> lines,
        bool taxIncluded,
        decimal taxRate,
        bool depositRequired,
        decimal depositPercent,
        string? shippingZoneCode,
        decimal shippingFee)
    {
        var sb = new StringBuilder();
        sb.Append(shopId.ToString("N")).Append('|');
        foreach (var line in lines.OrderBy(item => item.VariantId))
        {
            sb.Append(line.VariantId.ToString("N")).Append(':')
              .Append(line.Quantity.ToString(CultureInfo.InvariantCulture)).Append(':')
              .Append(line.OriginalUnitPrice.ToString("F2", CultureInfo.InvariantCulture)).Append(':')
              .Append(line.DiscountedUnitPrice.ToString("F2", CultureInfo.InvariantCulture)).Append(':')
              .Append(line.PromotionId?.ToString("N") ?? "none").Append(';');
        }

        sb.Append('|').Append(taxIncluded ? '1' : '0')
          .Append(':').Append(taxRate.ToString("F2", CultureInfo.InvariantCulture))
          .Append('|').Append(depositRequired ? '1' : '0')
          .Append(':').Append(depositPercent.ToString("F2", CultureInfo.InvariantCulture))
          .Append('|').Append((shippingZoneCode ?? "DEFAULT").Trim().ToUpperInvariant())
          .Append(':').Append(shippingFee.ToString("F2", CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return $"qv1_{Convert.ToHexString(hash)[..24].ToLowerInvariant()}";
    }

    public static IReadOnlyList<decimal> SplitIntoParts(decimal totalAmount, decimal maxPartAmount = OrderPolicyDefaults.VnPayMaxAmountPerPartVnd)
    {
        if (totalAmount <= 0m)
            return [];
        if (maxPartAmount <= 0m)
            maxPartAmount = OrderPolicyDefaults.VnPayMaxAmountPerPartVnd;

        var parts = new List<decimal>();
        var remaining = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
        while (remaining > 0m)
        {
            var chunk = remaining > maxPartAmount ? maxPartAmount : remaining;
            parts.Add(chunk);
            remaining -= chunk;
        }

        return parts;
    }
}

