using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.DataManagement.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>Permanently removes every player on a team, along with their suspensions and match events. The team itself is untouched. SuperAdmin only.</summary>
public sealed record ClearPlayersCommand(Guid TeamId) : IRequest<int>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ClearPlayersCommandHandler : IRequestHandler<ClearPlayersCommand, int>
{
    private readonly ILeagueDbContext _db;

    public ClearPlayersCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<int> Handle(ClearPlayersCommand request, CancellationToken cancellationToken)
    {
        var teamExists = await _db.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken);
        if (!teamExists)
            throw new NotFoundException(nameof(Domain.Entities.Team), request.TeamId);

        return await ClearDataHelpers.ClearPlayersForTeamAsync(_db, request.TeamId, cancellationToken);
    }
}
