using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto?>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly ILeagueDbContext _db;

    public GetUserByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDetailDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt,
                // Was `.ToList() as IReadOnlyList<int>` on a List<Role>, which is never that
                // type and so was null every time — the compiler warned about exactly this.
                u.UserRoles.Select(ur => (int)ur.Role).ToList(),
                u.UserTeams.Select(ut => ut.TeamId).ToList(),
                u.UserDivisions.Select(ud => ud.DivisionId).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}
