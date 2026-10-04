namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the Delivery domain entity.</summary>
public class Delivery : BaseEntity
{
    public Guid? ContractId { get; set; }

    public Guid? OrderId { get; set; }

    public string DeliveryNumber { get; set; } = string.Empty;

    public DateOnly? ScheduledDate { get; set; }

    public DateOnly? ActualDeliveryDate { get; set; }

    public string? FromAddress { get; set; }

    public string? ToAddress { get; set; }

    public string? RecipientName { get; set; }

    public string? RecipientPhone { get; set; }

    public string? TrackingCode { get; set; }

    public string? Notes { get; set; }

    public DeliveryType DeliveryType { get; set; } = DeliveryType.PlatformManaged;

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
}
