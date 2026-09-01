using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.DataManagement.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>Permanently removes a team and everything tied to it: players, suspensions, match events and match history. SuperAdmin only.</summary>
public sealed record ClearTeamCommand(Guid TeamId) : IRequest<int>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ClearTeamCommandHandler : IRequestHandler<ClearTeamCommand, int>
{
    private readonly ILeagueDbContext _db;

    public ClearTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<int> Handle(ClearTeamCommand request, CancellationToken cancellationToken)
    {
        var teamExists = await _db.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken);
        if (!teamExists)
            throw new NotFoundException(nameof(Domain.Entities.Team), request.TeamId);

        await ClearDataHelpers.ClearTeamDataAsync(_db, request.TeamId, cancellationToken);

        return await _db.Teams
            .Where(t => t.Id == request.TeamId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
