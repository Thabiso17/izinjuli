using iDiski.Domain.Entities;

namespace iDiski.Domain.Services;

/// <summary>
/// Pure static domain service that owns all standing-calculation rules.
/// No EF Core, no MediatR, no DI — fully unit-testable with plain C#.
///
/// Rules (standard football):
///   Win  → 3 pts   |   Draw → 1 pt each   |   Loss → 0 pts
/// Tie-break order: Points → GD → GF → Alphabetical (Name)
/// </summary>
public static class StandingsCalculator
{
    public sealed record TeamStanding(
        int     Position,
        Guid    TeamId,
        string  TeamName,
        string  ShortCode,
        string? LogoUrl,
        int     Played,
        int     Won,
        int     Drawn,
        int     Lost,
        int     GoalsFor,
        int     GoalsAgainst,
        int     GoalDifference,
        int     Points,
        string  Form            // last 5 results as "W,D,L,W,W"
    );

    /// <summary>
    /// Computes the full league table from a set of completed matches.
    /// All filtering (season, etc.) must be applied by the caller before passing matches in.
    /// </summary>
    public static IReadOnlyList<TeamStanding> Compute(
        IReadOnlyList<MatchResult> completedMatches,
        IReadOnlyList<Team> teams)
    {
        // Index matches per team for O(1) form lookups later
        var matchesByTeam = completedMatches
            .SelectMany(m => new[]
            {
                (TeamId: m.HomeTeamId, Match: m, IsHome: true),
                (TeamId: m.AwayTeamId, Match: m, IsHome: false)
            })
            .GroupBy(x => x.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var standings = teams.Select(team =>
        {
            var entries = matchesByTeam.GetValueOrDefault(team.Id)
                          ?? new List<(Guid, MatchResult, bool)>();

            int won = 0, drawn = 0, lost = 0, gf = 0, ga = 0;

            foreach (var (_, match, isHome) in entries)
            {
                int scored    = isHome ? match.HomeScore : match.AwayScore;
                int conceded  = isHome ? match.AwayScore : match.HomeScore;

                gf += scored;
                ga += conceded;

                if (scored > conceded)       won++;
                else if (scored == conceded)  drawn++;
                else                          lost++;
            }

            // Form: last 5 by MatchDate descending
            var recentForm = entries
                .OrderByDescending(e => e.Match.MatchDate)
                .Take(5)
                .Select(e =>
                {
                    bool isHome  = e.IsHome;
                    int scored   = isHome ? e.Match.HomeScore : e.Match.AwayScore;
                    int conceded = isHome ? e.Match.AwayScore : e.Match.HomeScore;
                    return scored > conceded ? "W" : scored == conceded ? "D" : "L";
                });

            return new
            {
                TeamId         = team.Id,
                TeamName       = team.Name,
                ShortCode      = team.ShortCode,
                LogoUrl        = team.LogoUrl,
                Played         = entries.Count,
                Won            = won,
                Drawn          = drawn,
                Lost           = lost,
                GoalsFor       = gf,
                GoalsAgainst   = ga,
                GoalDifference = gf - ga,
                Points         = (won * 3) + drawn,
                Form           = string.Join(",", recentForm)
            };
        })
        .OrderByDescending(s => s.Points)
        .ThenByDescending(s => s.GoalDifference)
        .ThenByDescending(s => s.GoalsFor)
        .ThenBy(s => s.TeamName)
        .ToList();

        // Assign positions after sorting
        return standings
            .Select((s, i) => new TeamStanding(
                Position:       i + 1,
                TeamId:         s.TeamId,
                TeamName:       s.TeamName,
                ShortCode:      s.ShortCode,
                LogoUrl:        s.LogoUrl,
                Played:         s.Played,
                Won:            s.Won,
                Drawn:          s.Drawn,
                Lost:           s.Lost,
                GoalsFor:       s.GoalsFor,
                GoalsAgainst:   s.GoalsAgainst,
                GoalDifference: s.GoalDifference,
                Points:         s.Points,
                Form:           s.Form))
            .ToList();
    }
}
