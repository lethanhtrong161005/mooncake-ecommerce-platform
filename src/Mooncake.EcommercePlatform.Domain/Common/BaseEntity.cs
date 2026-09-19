namespace Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Base class for all domain entities providing common audit fields.</summary>
public abstract class BaseEntity
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Soft-delete flag. True means the record is logically deleted.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>Identifier of the user who created this record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Identifier of the user who last updated this record.</summary>
    public string? UpdatedBy { get; set; }
}
