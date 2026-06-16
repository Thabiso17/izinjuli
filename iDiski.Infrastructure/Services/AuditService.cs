using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace iDiski.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ILeagueDbContext db,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string description,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = _currentUserService.UserId,
                UserEmail = _currentUserService.Email,
                OldValues = oldValues,
                NewValues = newValues,
                Description = description,
                ChangedAt = DateTime.UtcNow,
                SourceIp = GetClientIp()
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't throw from audit logging - it should never break the main operation
            System.Console.WriteLine($"⚠️ Audit logging failed: {ex.Message}");
        }
    }

    private string? GetClientIp()
    {
        try
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null)
                return null;

            // Try to get the client IP from common headers
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                return forwardedFor.ToString().Split(',').First().Trim();

            if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
                return realIp.ToString();

            return context.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
