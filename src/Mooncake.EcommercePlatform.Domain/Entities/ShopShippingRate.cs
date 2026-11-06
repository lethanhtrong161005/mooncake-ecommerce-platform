namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Configures regional shipping fees and lead times for a supplier shop.</summary>
public sealed class ShopShippingRate : BaseEntity
{
    public Guid ShopId { get; set; }

    public string RegionCode { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public decimal FeeAmount { get; set; }

    public decimal? FreeShippingThreshold { get; set; }

    public int EstimatedDays { get; set; } = 2;

    public bool IsSupported { get; set; } = true;
}

