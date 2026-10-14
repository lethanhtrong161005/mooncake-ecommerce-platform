namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Stores an administrator-managed platform setting as JSON.</summary>
public sealed class SystemConfig : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "null";
    public string? Description { get; set; }
}
