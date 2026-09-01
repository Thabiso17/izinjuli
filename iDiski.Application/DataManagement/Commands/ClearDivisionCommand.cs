using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.DataManagement.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>Permanently removes a division and everything nested under it: teams, players, suspensions, match events and match history. Returns the number of teams removed. SuperAdmin only.</summary>
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

        // Safety net: a match can carry a DivisionId independent of its two teams' own
        // division assignment. Clear those before removing the division itself.
        await _db.MatchResults
            .Where(m => m.DivisionId == request.DivisionId)
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
