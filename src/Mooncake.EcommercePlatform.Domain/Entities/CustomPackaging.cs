using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class CustomPackaging : BaseEntity
{
    public long CustomerId { get; set; }
    public string Name { get; set; } = default!;
    public string LogoUrl { get; set; } = default!;
    public string? DesignNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Customer? Customer { get; set; }
}
