using iDiski.Domain.Entities;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Standings.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record HeadToHeadMatchDto(
    DateTime    MatchDate,
    int         Season,
    int         Matchweek,
    string      HomeTeamName,
    string      AwayTeamName,
    int         HomeScore,
    int         AwayScore,
    string      Result          // "HOME_WIN" | "AWAY_WIN" | "DRAW"
);

public sealed record HeadToHeadDto(
    Guid   TeamAId,
    string TeamAName,
    string TeamAShortCode,
    Guid   TeamBId,
    string TeamBName,
    string TeamBShortCode,

    int TotalMeetings,
    int TeamAWins,
    int TeamBWins,
    int Draws,

    int TeamAGoalsScored,
    int TeamBGoalsScored,

    IReadOnlyList<HeadToHeadMatchDto> RecentMeetings   // newest first, capped at 10
);

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetHeadToHeadQuery(
    Guid TeamAId,
    Guid TeamBId
) : IRequest<HeadToHeadDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetHeadToHeadQueryHandler
    : IRequestHandler<GetHeadToHeadQuery, HeadToHeadDto>
{
    private readonly ILeagueDbContext _db;

    public GetHeadToHeadQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<HeadToHeadDto> Handle(
        GetHeadToHeadQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TeamAId == request.TeamBId)
            throw new ArgumentException("TeamA and TeamB must be different teams.");

        // Validate both teams exist
        var teamA = await _db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamAId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.TeamAId);

        var teamB = await _db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamBId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.TeamBId);

        // Load all completed meetings between the two teams (either direction)
        var meetings = await _db.MatchResults
            .AsNoTracking()
            .Where(m =>
                m.Status == MatchStatus.Completed &&
                ((m.HomeTeamId == request.TeamAId && m.AwayTeamId == request.TeamBId) ||
                 (m.HomeTeamId == request.TeamBId && m.AwayTeamId == request.TeamAId)))
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync(cancellationToken);

        int teamAWins = 0, teamBWins = 0, draws = 0;
        int teamAGoals = 0, teamBGoals = 0;

        foreach (var m in meetings)
        {
            bool teamAIsHome = m.HomeTeamId == request.TeamAId;

            int aScore = teamAIsHome ? m.HomeScore : m.AwayScore;
            int bScore = teamAIsHome ? m.AwayScore : m.HomeScore;

            teamAGoals += aScore;
            teamBGoals += bScore;

            if (aScore > bScore)       teamAWins++;
            else if (aScore < bScore)  teamBWins++;
            else                        draws++;
        }

        var recentMeetings = meetings
            .Take(10)
            .Select(m =>
            {
                var homeTeamName = m.HomeTeamId == request.TeamAId ? teamA.Name : teamB.Name;
                var awayTeamName = m.AwayTeamId == request.TeamAId ? teamA.Name : teamB.Name;
                var result = m.HomeScore > m.AwayScore ? "HOME_WIN"
                           : m.HomeScore < m.AwayScore ? "AWAY_WIN"
                           : "DRAW";

                return new HeadToHeadMatchDto(
                    m.MatchDate, m.Season, m.MatchweekNumber,
                    homeTeamName, awayTeamName,
                    m.HomeScore, m.AwayScore, result);
            })
            .ToList();

        return new HeadToHeadDto(
            TeamAId:          teamA.Id,
            TeamAName:        teamA.Name,
            TeamAShortCode:   teamA.ShortCode,
            TeamBId:          teamB.Id,
            TeamBName:        teamB.Name,
            TeamBShortCode:   teamB.ShortCode,
            TotalMeetings:    meetings.Count,
            TeamAWins:        teamAWins,
            TeamBWins:        teamBWins,
            Draws:            draws,
            TeamAGoalsScored: teamAGoals,
            TeamBGoalsScored: teamBGoals,
            RecentMeetings:   recentMeetings);
    }
}
