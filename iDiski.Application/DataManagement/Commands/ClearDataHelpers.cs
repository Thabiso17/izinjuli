using iDiski.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.DataManagement.Commands;

/// <summary>
/// Shared dependency-ordered bulk-delete steps for the "Clear Data" admin feature.
/// FK relationships between Player/Suspension/MatchEvent/MatchResult are all
/// DeleteBehavior.Restrict (see LeagueDbContext.OnModelCreating), so children must
/// be removed before their parents.
/// </summary>
internal static class ClearDataHelpers
{
    /// <summary>Removes every player on a team, plus their suspensions and match events. Returns the number of players removed.</summary>
    public static async Task<int> ClearPlayersForTeamAsync(
        ILeagueDbContext db, Guid teamId, CancellationToken cancellationToken)
    {
        var playerIds = db.Players.Where(p => p.TeamId == teamId).Select(p => p.Id);

        await db.Suspensions
            .Where(s => playerIds.Contains(s.PlayerId))
            .ExecuteDeleteAsync(cancellationToken);

        await db.MatchEvents
            .Where(e => playerIds.Contains(e.PlayerId))
            .ExecuteDeleteAsync(cancellationToken);

        return await db.Players
            .Where(p => p.TeamId == teamId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Clears a team's players and match history, leaving the team itself in place.</summary>
    public static async Task ClearTeamDataAsync(
        ILeagueDbContext db, Guid teamId, CancellationToken cancellationToken)
    {
        await ClearPlayersForTeamAsync(db, teamId, cancellationToken);

        // MatchEvents belonging to these matches cascade-delete at the DB level
        // (MatchEvent -> MatchResult is configured as DeleteBehavior.Cascade).
        await db.MatchResults
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
