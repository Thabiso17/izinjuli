using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(
    string? EntityType = null,
    Guid? EntityId = null,
    Guid? UserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<IReadOnlyList<AuditLogDto>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAuditLogsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(al => al.EntityType == request.EntityType);

        if (request.EntityId.HasValue)
            query = query.Where(al => al.EntityId == request.EntityId);

        if (request.UserId.HasValue)
            query = query.Where(al => al.UserId == request.UserId);

        if (request.FromDate.HasValue)
            query = query.Where(al => al.ChangedAt >= request.FromDate);

        if (request.ToDate.HasValue)
            query = query.Where(al => al.ChangedAt <= request.ToDate);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and sort
        var auditLogs = await query
            .OrderByDescending(al => al.ChangedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(al => new AuditLogDto(
                al.Id,
                al.EntityType,
                al.EntityId,
                al.Action,
                al.UserId,
                al.UserEmail,
                al.Description,
                al.OldValues,
                al.NewValues,
                al.ChangedAt,
                al.SourceIp
            ))
            .ToListAsync(cancellationToken);

        return auditLogs;
    }
}
