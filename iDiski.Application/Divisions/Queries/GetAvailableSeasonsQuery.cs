using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Divisions.Queries;

/// <summary>
/// Returns a distinct list of all seasons that have divisions in the database,
/// ordered descending (most recent first).
/// </summary>
public record GetAvailableSeasonsQuery : IRequest<IReadOnlyList<int>>;

public class GetAvailableSeasonsQueryHandler : IRequestHandler<GetAvailableSeasonsQuery, IReadOnlyList<int>>
{
    private readonly ILeagueDbContext _db;

    public GetAvailableSeasonsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<int>> Handle(GetAvailableSeasonsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Divisions
            .AsNoTracking()
            .Select(d => d.Season)
            .Distinct()
            .OrderByDescending(s => s)
            .ToListAsync(cancellationToken);
    }
}
