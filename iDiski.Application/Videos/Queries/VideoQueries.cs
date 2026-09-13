using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Videos.Queries;

// ═════════════════════════════════════════════════════════════════════════════
// GET PUBLISHED VIDEOS (for public homepage)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetPublishedVideosQuery(
    int   Limit      = 10,
    Guid? DivisionId = null,
    Guid? TeamId     = null,
    Guid? PlayerId   = null
) : IRequest<List<VideoSummaryDto>>;

public sealed class GetPublishedVideosQueryHandler
    : IRequestHandler<GetPublishedVideosQuery, List<VideoSummaryDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPublishedVideosQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<List<VideoSummaryDto>> Handle(
        GetPublishedVideosQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Videos
            .AsNoTracking()
            .Where(v => v.IsPublished && !v.IsArchived);

        if (request.DivisionId.HasValue)
            query = query.Where(v => v.DivisionId == request.DivisionId.Value);

        if (request.TeamId.HasValue)
            query = query.Where(v => v.TeamId == request.TeamId.Value);

        if (request.PlayerId.HasValue)
            query = query.Where(v => v.PlayerId == request.PlayerId.Value);

        return await query
            .OrderByDescending(v => v.IsPinned)      // Pinned first
            .ThenByDescending(v => v.PublishedAt)    // Then by most recent
            .Take(request.Limit)
            .Select(v => new VideoSummaryDto(
                v.Id,
                v.Title,
                v.VideoUrl,
                v.Description,
                v.ThumbnailUrl,
                v.Author,
                v.PublishedAt,
                v.IsPinned))
            .ToListAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GET ALL VIDEOS (admin panel)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetAllVideosAdminQuery(
    bool? PublishedOnly = null
) : IRequest<List<VideoSummaryDto>>;

public sealed class GetAllVideosAdminQueryHandler
    : IRequestHandler<GetAllVideosAdminQuery, List<VideoSummaryDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAllVideosAdminQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<List<VideoSummaryDto>> Handle(
        GetAllVideosAdminQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Videos.AsNoTracking();

        if (request.PublishedOnly.HasValue)
            query = query.Where(v => v.IsPublished == request.PublishedOnly.Value);

        return await query
            .OrderByDescending(v => v.IsPinned)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => new VideoSummaryDto(
                v.Id,
                v.Title,
                v.VideoUrl,
                v.Description,
                v.ThumbnailUrl,
                v.Author,
                v.PublishedAt,
                v.IsPinned,
                v.IsArchived))
            .ToListAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GET VIDEO BY ID
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetVideoByIdQuery(Guid Id) : IRequest<VideoDto>;

public sealed class GetVideoByIdQueryHandler : IRequestHandler<GetVideoByIdQuery, VideoDto>
{
    private readonly ILeagueDbContext _db;

    public GetVideoByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<VideoDto> Handle(GetVideoByIdQuery request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos
            .AsNoTracking()
            .Where(v => v.Id == request.Id)
            .Select(v => new VideoDto(
                v.Id,
                v.Title,
                v.VideoUrl,
                v.Description,
                v.ThumbnailUrl,
                v.Author,
                v.IsPublished,
                v.PublishedAt,
                v.IsPinned,
                v.ViewCount,
                v.DivisionId,
                v.TeamId,
                v.PlayerId))
            .FirstOrDefaultAsync(cancellationToken);

        return video ?? throw new NotFoundException(nameof(Video), request.Id);
    }
}
