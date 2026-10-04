namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the RfqItem domain entity.</summary>
public class RfqItem : BaseEntity
{
    public Guid RfqId { get; set; }

    public string? ProductName { get; set; }

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal? TargetPrice { get; set; }

    public string? Specifications { get; set; }

    public string? PackagingRequirements { get; set; }

    public bool NeedsCustomPackaging { get; set; }
}
