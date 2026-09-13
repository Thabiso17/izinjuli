using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Teams.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

// The CanManageDivisions policy on the endpoint keeps TeamAdmins out entirely; this scopes
// a DivisionAdmin to teams that sit inside a division they are assigned to.
public sealed record DeleteTeamCommand(Guid Id) : IRequest, IRequireTeamAccess
{
    Guid IRequireTeamAccess.TeamId => Id;
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand>
{
    private readonly ILeagueDbContext _db;

    public DeleteTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(
        DeleteTeamCommand request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Team), request.Id);

        var hasMatches = await _db.MatchResults
            .AnyAsync(m => m.HomeTeamId == request.Id || m.AwayTeamId == request.Id,
                      cancellationToken);

        if (hasMatches)
            throw new InvalidOperationException(
                "Cannot delete a team that has match history. Archive the team instead.");

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
