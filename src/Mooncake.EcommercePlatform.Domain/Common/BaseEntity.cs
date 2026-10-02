namespace Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Base class for all domain entities — provides only the identity column.</summary>
public abstract class BaseEntity
{
    /// <summary>Primary key (auto-increment BIGINT).</summary>
    public long Id { get; set; }
}
