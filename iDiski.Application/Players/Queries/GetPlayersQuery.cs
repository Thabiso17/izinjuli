using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Players.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <param name="TeamId">Filter by team. If null, returns all players in the league.</param>
/// <param name="ActiveOnly">When true, excludes released/inactive players.</param>
public sealed record GetPlayersQuery(
    Guid? TeamId     = null,
    bool  ActiveOnly = true
) : IRequest<IReadOnlyList<PlayerDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetPlayersQueryHandler
    : IRequestHandler<GetPlayersQuery, IReadOnlyList<PlayerDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPlayersQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlayerDto>> Handle(
        GetPlayersQuery request,
        CancellationToken cancellationToken)
    {
        // Validate team exists when filtering by team
        if (request.TeamId.HasValue)
        {
            var teamExists = await _db.Teams
                .AnyAsync(t => t.Id == request.TeamId.Value, cancellationToken);

            if (!teamExists)
                throw new NotFoundException(nameof(Team), request.TeamId.Value);
        }

        var query = _db.Players
            .AsNoTracking()
            .Include(p => p.Team)
            .AsQueryable();

        if (request.TeamId.HasValue)
            query = query.Where(p => p.TeamId == request.TeamId.Value);

        if (request.ActiveOnly)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.JerseyNumber)
            .Select(p => new PlayerDto(
                p.Id,
                p.FirstName,
                p.LastName,
                p.FirstName + " " + p.LastName,
                p.ProfileImageUrl,
                p.Bio,
                p.DateOfBirth,
                DateTime.UtcNow.Year - p.DateOfBirth.Year,
                p.Nationality,
                p.JerseyNumber,
                p.Position,
                p.PreferredFoot,
                p.IsActive,
                p.Goals,
                p.Assists,
                p.YellowCards,
                p.RedCards,
                p.TeamId,
                p.Team.Name))
            .ToListAsync(cancellationToken);
    }
}
