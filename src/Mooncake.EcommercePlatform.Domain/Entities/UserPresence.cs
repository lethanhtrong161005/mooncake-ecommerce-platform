namespace Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persisted last-seen timestamp and privacy preference; live online state is kept in memory.</summary>
public class UserPresence
{
    public Guid UserId { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public bool ShowActiveStatus { get; set; } = true;
}

