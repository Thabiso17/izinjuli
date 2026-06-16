namespace iDiski.Application.Common.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Record an audit log entry for an entity change.
    /// </summary>
    /// <param name="entityType">Type name of the entity (e.g., "Team", "Player")</param>
    /// <param name="entityId">ID of the entity that changed</param>
    /// <param name="action">Action performed: "Created", "Updated", or "Deleted"</param>
    /// <param name="description">Human-readable description of the change</param>
    /// <param name="oldValues">Optional JSON of previous values (for updates/deletes)</param>
    /// <param name="newValues">Optional JSON of new values (for creates/updates)</param>
    /// <param name="cancellationToken"></param>
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string description,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default);
}
