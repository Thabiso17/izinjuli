using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.DataManagement.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>Permanently removes a division and everything belonging to it: its teams, their
/// players, suspensions, match events and fixtures, and the places those clubs held in any
/// competition. The competitions themselves survive — a division does not own one. Returns the
/// number of teams removed. SuperAdmin only.</summary>
public sealed record ClearDivisionCommand(Guid DivisionId) : IRequest<int>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ClearDivisionCommandHandler : IRequestHandler<ClearDivisionCommand, int>
{
    private readonly ILeagueDbContext _db;

    public ClearDivisionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<int> Handle(ClearDivisionCommand request, CancellationToken cancellationToken)
    {
        var divisionExists = await _db.Divisions.AnyAsync(d => d.Id == request.DivisionId, cancellationToken);
        if (!divisionExists)
            throw new NotFoundException(nameof(Domain.Entities.Division), request.DivisionId);

        var teamIds = await _db.Teams
            .Where(t => t.DivisionId == request.DivisionId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var teamId in teamIds)
        {
            await ClearDataHelpers.ClearTeamDataAsync(_db, teamId, cancellationToken);
        }

        // Any fixture either of this division's clubs was involved in, whatever competition it
        // was part of. ClearTeamDataAsync above covers each club's own matches; this catches a
        // fixture whose other side has already gone.
        //
        // Matched on the ids gathered above rather than through the navigation: ExecuteDelete
        // issues one DELETE and cannot always translate a predicate that needs a join.
        await _db.MatchResults
            .Where(m =>
                (m.HomeTeamId != null && teamIds.Contains(m.HomeTeamId.Value)) ||
                (m.AwayTeamId != null && teamIds.Contains(m.AwayTeamId.Value)))
            .ExecuteDeleteAsync(cancellationToken);

        // The places this division's clubs held in competitions. The competitions themselves
        // stay: a division does not own one, and a cup contested across three divisions is
        // not abandoned because one of them was cleared — it is left with fewer entrants,
        // which is the truth of what happened.
        await _db.CompetitionEntries
            .Where(e => teamIds.Contains(e.TeamId))
            .ExecuteDeleteAsync(cancellationToken);

        var teamsRemoved = await _db.Teams
            .Where(t => t.DivisionId == request.DivisionId)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.Divisions
            .Where(d => d.Id == request.DivisionId)
            .ExecuteDeleteAsync(cancellationToken);

        return teamsRemoved;
    }
}
