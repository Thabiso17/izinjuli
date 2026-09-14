using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Authentication.Queries;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

/// <param name="AdministeredDivisionIds">
/// The divisions this user is assigned to, and <paramref name="AdministeredTeamIds"/> the
/// teams. Roles alone say what kind of administrator somebody is, never which competitions
/// they administer — so every admin screen showed every division and every club, with an Edit
/// button on rows the API would refuse to save. Empty for a super admin, who administers
/// everything and is told so by <paramref name="IsSuperAdmin"/> rather than by a list.
/// </param>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? ProfileImageUrl,
    string[] Roles,
    bool IsSuperAdmin,
    Guid[] AdministeredDivisionIds,
    Guid[] AdministeredTeamIds
);

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        ILeagueDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // 1. Get current user ID from context (set by JWT middleware)
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == null)
            throw new UnauthorizedException("User is not authenticated");

        // 2. Fetch user with roles
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserDivisions)
            .Include(u => u.UserTeams)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), _currentUserService.UserId);

        // 3. Map roles
        var roles = user.UserRoles
            .Select(ur => ur.Role.ToString())
            .ToArray();

        var isSuperAdmin = user.UserRoles.Any(ur => ur.Role == Domain.Enums.Role.SuperAdmin);

        // 4. Return DTO
        return new CurrentUserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            ProfileImageUrl: user.ProfileImageUrl,
            Roles: roles,
            IsSuperAdmin: isSuperAdmin,
            // Left empty for a super admin rather than listing every division in the league:
            // they are not scoped to a set, and a caller that treated the list as the scope
            // would silently narrow them the moment a new division was created.
            AdministeredDivisionIds: isSuperAdmin
                ? Array.Empty<Guid>()
                : user.UserDivisions.Select(ud => ud.DivisionId).ToArray(),
            AdministeredTeamIds: isSuperAdmin
                ? Array.Empty<Guid>()
                : user.UserTeams.Select(ut => ut.TeamId).ToArray()
        );
    }
}
