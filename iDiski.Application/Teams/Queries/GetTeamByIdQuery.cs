using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Teams.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetTeamByIdQuery(Guid Id) : IRequest<TeamDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetTeamByIdQueryHandler
    : IRequestHandler<GetTeamByIdQuery, TeamDto>
{
    private readonly ILeagueDbContext _db;

    public GetTeamByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<TeamDto> Handle(
        GetTeamByIdQuery request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.ShortCode,
                t.LogoUrl,
                t.Founded,
                t.HomeGround,
                t.City,
                t.PrimaryColour,
                t.SecondaryColour,
                t.Players.Count(p => p.IsActive)))
            .FirstOrDefaultAsync(cancellationToken);

        return team ?? throw new NotFoundException(nameof(Domain.Entities.Team), request.Id);
    }
}
