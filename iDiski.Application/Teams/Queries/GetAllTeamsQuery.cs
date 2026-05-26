using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Teams.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetAllTeamsQuery : IRequest<IReadOnlyList<TeamDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetAllTeamsQueryHandler
    : IRequestHandler<GetAllTeamsQuery, IReadOnlyList<TeamDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAllTeamsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<TeamDto>> Handle(
        GetAllTeamsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Teams
            .AsNoTracking()
            .Include(t => t.Division)
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.ShortCode,
                t.LogoUrl,
                t.Founded,
                t.HomeGround,
                t.City,
                t.PrimaryColour,
                t.SecondaryColour,
                t.Players.Count(p => p.IsActive),
                t.DivisionId,
                t.Division != null ? t.Division.Name : null))
            .ToListAsync(cancellationToken);
    }
}
