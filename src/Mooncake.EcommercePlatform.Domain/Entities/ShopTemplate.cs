using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class ShopTemplate : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? PreviewUrl { get; set; }
    public string Config { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
