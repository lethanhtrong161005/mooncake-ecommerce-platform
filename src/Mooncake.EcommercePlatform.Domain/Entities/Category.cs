using Mooncake.EcommercePlatform.Domain.Common;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Category : BaseEntity
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
}
