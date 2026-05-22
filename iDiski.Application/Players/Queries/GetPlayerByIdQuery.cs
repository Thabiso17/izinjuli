using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Players.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetPlayerByIdQuery(Guid Id) : IRequest<PlayerDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetPlayerByIdQueryHandler
    : IRequestHandler<GetPlayerByIdQuery, PlayerDto>
{
    private readonly ILeagueDbContext _db;

    public GetPlayerByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<PlayerDto> Handle(
        GetPlayerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var player = await _db.Players
            .AsNoTracking()
            .Include(p => p.Team)
            .Where(p => p.Id == request.Id)
            .Select(p => new PlayerDto(
                p.Id,
                p.FirstName,
                p.LastName,
                p.FirstName + " " + p.LastName,
                p.ProfileImageUrl,
                p.Bio,
                p.DateOfBirth,
                DateTime.UtcNow.Year - p.DateOfBirth.Year,
                p.Nationality,
                p.JerseyNumber,
                p.Position,
                p.PreferredFoot,
                p.IsActive,
                p.Goals,
                p.Assists,
                p.YellowCards,
                p.RedCards,
                p.TeamId,
                p.Team.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return player ?? throw new NotFoundException(nameof(Domain.Entities.Player), request.Id);
    }
}
