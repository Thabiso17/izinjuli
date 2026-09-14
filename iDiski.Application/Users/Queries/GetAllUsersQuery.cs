using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Queries;

public sealed record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAllUsersQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            // Ordered before projecting, not after. Sorting the projection makes EF re-evaluate
            // the constructor to reach a field, and with the roles subquery inside it that is
            // not translatable — the whole endpoint failed, which is why the administrators
            // list came back empty rather than merely unsorted.
            .OrderBy(u => u.Email)
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt,
                u.UserRoles.Select(r => (int)r.Role).ToList()
            ))
            .ToListAsync(cancellationToken);

        return users;
    }
}
