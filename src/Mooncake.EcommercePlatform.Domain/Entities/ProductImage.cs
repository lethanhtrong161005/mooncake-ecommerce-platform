using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class ProductImage : BaseEntity
{
    public long ProductId { get; set; }
    public string Url { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Product? Product { get; set; }
}
