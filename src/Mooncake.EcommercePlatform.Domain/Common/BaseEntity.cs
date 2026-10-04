namespace Mooncake.EcommercePlatform.Domain.Common;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>Base class for all domain entities providing common audit and identity fields.</summary>
public abstract class BaseEntity
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Soft-delete flag. True means the record is logically deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Alias for CreatedAt adhering to UTC naming standard.</summary>
    [NotMapped]
    public DateTime CreatedAtUtc
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    /// <summary>Alias for UpdatedAt adhering to UTC naming standard.</summary>
    [NotMapped]
    public DateTime UpdatedAtUtc
    {
        get => UpdatedAt;
        set => UpdatedAt = value;
    }

    /// <summary>Identifier of the user who created this record.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Identifier of the user who last updated this record.</summary>
    public Guid? UpdatedBy { get; set; }
}
