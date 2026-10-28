namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Immutable audit entry for a committed product variant inventory delta.</summary>
public sealed class ProductInventoryMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public Guid ShopId { get; set; }
    public Guid ActorUserId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public int Delta { get; set; }
    public int PreviousQty { get; set; }
    public int CurrentQty { get; set; }
    public string Reason { get; set; } = string.Empty;
}
