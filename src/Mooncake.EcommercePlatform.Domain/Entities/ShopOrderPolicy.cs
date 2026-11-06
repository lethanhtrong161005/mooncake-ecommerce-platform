namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Configures deposit, tax, and shipping rules for direct bulk orders on a shop.</summary>
public sealed class ShopOrderPolicy : BaseEntity
{
    public Guid ShopId { get; set; }

    public bool DepositEnabled { get; set; }

    public decimal MinimumOrderTotalForDeposit { get; set; }

    public decimal DepositPercent { get; set; } = 30m;

    public bool TaxIncluded { get; set; } = true;

    public decimal TaxRate { get; set; } = 8m;

    public bool AllowSelfPickup { get; set; } = true;

    public bool RequireShippingZoneSelection { get; set; }

    public long Version { get; set; } = 1L;
}

