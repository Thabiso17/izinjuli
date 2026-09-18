using iDiski.Domain.Entities;
using iDiski.Domain.Services;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Standings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Standings.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <param name="Season">The league season year, e.g. 2025.</param>
/// <param name="CompetitionId">
/// The competition whose table this is, and the way this query should normally be asked.
///
/// Entrants then come from the entry list rather than from who happens to share a division,
/// which is what makes a table of twelve of a division's twenty possible — and what stops a
/// club invited from another division being left out of a table they are playing in.
/// </param>
/// <param name="DivisionId">
/// Optional division filter, kept for the league-wide view. It spans every competition being
/// run in the division, so it is a summary rather than any one competition's table.
/// </param>
/// <param name="UpToMatchweek">
/// Optional ceiling. When set, only matches up to and including this matchweek
/// are counted — useful for historical snapshots ("how did the table look after MW10?").
/// </param>
/// <param name="GroupName">
/// One group of a group stage, or null for the whole division. Merging the groups into a
/// single table ranks teams that have never played each other, which is not a table of
/// anything.
/// </param>
public sealed record GetLeagueStandingsQuery(
    int     Season,
    Guid?   DivisionId = null,
    int?    UpToMatchweek = null,
    string? GroupName = null,
    Guid?   CompetitionId = null
) : IRequest<LeagueTableDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetLeagueStandingsQueryHandler
    : IRequestHandler<GetLeagueStandingsQuery, LeagueTableDto>
{
    private readonly ILeagueDbContext _db;

    public GetLeagueStandingsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<LeagueTableDto> Handle(
        GetLeagueStandingsQuery request,
        CancellationToken cancellationToken)
    {
        // ── Who is in this table ──────────────────────────────────────────────
        //
        // A competition's entrants are the clubs entered in it. Division membership is a
        // different question and cannot answer this one: a cup may be eight of a division's
        // twenty, and may field clubs from three other divisions besides.
        List<Team> teams;

        if (request.CompetitionId.HasValue)
        {
            teams = await _db.CompetitionEntries
                .AsNoTracking()
                .Where(e => e.CompetitionId == request.CompetitionId.Value)
                .Select(e => e.Team)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var teamsQuery = _db.Teams.AsNoTracking();

            if (request.DivisionId.HasValue)
                teamsQuery = teamsQuery.Where(t => t.DivisionId == request.DivisionId.Value);

            teams = await teamsQuery.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        }

        // ── Load completed matches for the season/division ─────────────────────────────
        var matchQuery = _db.MatchResults
            .AsNoTracking()
            .Where(m => m.Season == request.Season &&
                        m.Status == MatchStatus.Completed &&
                        // A table is made of matches played for points. Winning a semi-final
                        // takes you to the final, not up the league, so bracket fixtures are
                        // left out of it; group fixtures are a league in miniature and count.
                        m.Stage != MatchStage.Knockout);

        if (request.CompetitionId.HasValue)
            matchQuery = matchQuery.Where(m => m.CompetitionId == request.CompetitionId.Value);
        else if (request.DivisionId.HasValue)
        {
            // A fixture has no division, so this means the matches this division's clubs
            // played. A table honestly belongs to a competition; this is the fallback for a
            // caller that only knows the division.
            matchQuery = matchQuery.Where(m =>
                (m.HomeTeam != null && m.HomeTeam.DivisionId == request.DivisionId.Value) ||
                (m.AwayTeam != null && m.AwayTeam.DivisionId == request.DivisionId.Value));
        }

        if (request.UpToMatchweek.HasValue)
            matchQuery = matchQuery.Where(m => m.MatchweekNumber <= request.UpToMatchweek.Value);

        if (!string.IsNullOrWhiteSpace(request.GroupName))
        {
            matchQuery = matchQuery.Where(m => m.GroupName == request.GroupName);

            // Which teams are in a group is carried by its fixtures, not by the team, so the
            // entrants are whoever was drawn to play in it. Taken from every fixture rather
            // than the completed ones, or a side that has not played yet would be missing
            // from its own group's table instead of sitting bottom of it on nought.
            var groupFixtures = _db.MatchResults
                .AsNoTracking()
                .Where(m => m.Season == request.Season && m.GroupName == request.GroupName);

            if (request.CompetitionId.HasValue)
            {
                groupFixtures = groupFixtures
                    .Where(m => m.CompetitionId == request.CompetitionId.Value);
            }
            else if (request.DivisionId.HasValue)
            {
                groupFixtures = groupFixtures.Where(m =>
                    (m.HomeTeam != null && m.HomeTeam.DivisionId == request.DivisionId.Value) ||
                    (m.AwayTeam != null && m.AwayTeam.DivisionId == request.DivisionId.Value));
            }

            var drawn = await groupFixtures
                .Select(m => new { m.HomeTeamId, m.AwayTeamId })
                .ToListAsync(cancellationToken);

            var entrants = drawn
                .SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            teams = teams.Where(t => entrants.Contains(t.Id)).ToList();
        }

        var completedMatches = await matchQuery.ToListAsync(cancellationToken);

        // ── Delegate all calculation to the domain service ────────────────────
        var table = StandingsCalculator.Compute(completedMatches, teams);

        int matchweeksPlayed = completedMatches.Any()
            ? completedMatches.Max(m => m.MatchweekNumber)
            : 0;

        // ── Map domain records → DTOs ─────────────────────────────────────────
        var standingDtos = table.Select(s => new StandingDto(
            s.Position,
            s.TeamId,
            s.TeamName,
            s.ShortCode,
            s.LogoUrl,
            s.Played,
            s.Won,
            s.Drawn,
            s.Lost,
            s.GoalsFor,
            s.GoalsAgainst,
            s.GoalDifference,
            s.Points,
            s.Form
        )).ToList();

        return new LeagueTableDto(
            Season:           request.Season,
            MatchweeksPlayed: matchweeksPlayed,
            Table:            standingDtos);
    }
}
