using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Standings.Queries;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed record TopScorerDto(
    int     Rank,
    Guid    PlayerId,
    string  FullName,
    string? ProfileImageUrl,
    string  TeamName,
    string  TeamShortCode,
    string? Position,
    int     Goals,
    int     Assists,
    /// <summary>Goals + Assists — useful secondary sort for the assists leaderboard.</summary>
    int     GoalContributions
);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <param name="Season">
/// Season year. Note: player Goals/Assists on the entity are cumulative,
/// so Season is used here as a label only — extend if you add per-season stat tracking.
/// </param>
/// <param name="TopN">How many rows to return. Default 10.</param>
public sealed record GetTopScorersQuery(
    int Season,
    int TopN = 10
) : IRequest<IReadOnlyList<TopScorerDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetTopScorersQueryHandler
    : IRequestHandler<GetTopScorersQuery, IReadOnlyList<TopScorerDto>>
{
    private readonly ILeagueDbContext _db;

    public GetTopScorersQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopScorerDto>> Handle(
        GetTopScorersQuery request,
        CancellationToken cancellationToken)
    {
        var scorers = await _db.Players
            .AsNoTracking()
            .Include(p => p.Team)
            .Where(p => p.Goals > 0 || p.Assists > 0)
            .OrderByDescending(p => p.Goals)
            .ThenByDescending(p => p.Assists)
            .ThenBy(p => p.LastName)
            .Take(request.TopN)
            .ToListAsync(cancellationToken);

        return scorers
            .Select((p, i) => new TopScorerDto(
                Rank:              i + 1,
                PlayerId:          p.Id,
                FullName:          $"{p.FirstName} {p.LastName}",
                ProfileImageUrl:   p.ProfileImageUrl,
                TeamName:          p.Team.Name,
                TeamShortCode:     p.Team.ShortCode,
                Position:          p.Position.ToString(),
                Goals:             p.Goals,
                Assists:           p.Assists,
                GoalContributions: p.Goals + p.Assists))
            .ToList();
    }
}
