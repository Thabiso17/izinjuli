namespace iDiski.Application.AuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    Guid? UserId,
    string? UserEmail,
    string? Description,
    string? OldValues,
    string? NewValues,
    DateTime ChangedAt,
    string? SourceIp
);
