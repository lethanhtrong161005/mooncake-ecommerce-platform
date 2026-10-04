namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the PromotionRule domain entity.</summary>
public class PromotionRule : BaseEntity
{
    public Guid ShopId { get; set; }

    public Guid? ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? MinQty { get; set; }

    public int? MaxQty { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
}
