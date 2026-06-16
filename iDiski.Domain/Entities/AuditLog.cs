namespace iDiski.Domain.Entities;

/// <summary>
/// Tracks all changes made to entities in the system.
/// Records who did what, when, and to which entity.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Entity type name (e.g., "Team", "Player", "Match")</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID of the entity that was modified</summary>
    public Guid EntityId { get; set; }

    /// <summary>The action performed: Created, Updated, Deleted</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>User ID who made the change</summary>
    public Guid? UserId { get; set; }

    /// <summary>User email for easy identification</summary>
    public string? UserEmail { get; set; }

    /// <summary>JSON snapshot of the entity before the change (for updates/deletes)</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the entity after the change (for creates/updates)</summary>
    public string? NewValues { get; set; }

    /// <summary>Human-readable description of the change</summary>
    public string? Description { get; set; }

    /// <summary>Timestamp of when the change occurred</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>IP address or source of the request (if available)</summary>
    public string? SourceIp { get; set; }
}
