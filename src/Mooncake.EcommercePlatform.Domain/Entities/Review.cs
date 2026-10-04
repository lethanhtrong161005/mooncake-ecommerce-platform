namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents the Review domain entity.</summary>
public class Review : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? OrderId { get; set; }

    public short? OverallRating { get; set; }

    public short? QualityRating { get; set; }

    public short? DeliveryRating { get; set; }

    public short? ServiceRating { get; set; }

    public string? Comment { get; set; }

    public bool IsPublic { get; set; }
}
