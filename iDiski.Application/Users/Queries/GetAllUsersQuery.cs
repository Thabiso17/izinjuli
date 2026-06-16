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
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt
            ))
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        return users;
    }
}
