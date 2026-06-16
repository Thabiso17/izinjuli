using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Authorization;

public class TeamOwnershipHandler : AuthorizationHandler<TeamOwnershipRequirement>
{
    private readonly ILeagueDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TeamOwnershipHandler(ILeagueDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamOwnershipRequirement requirement)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            context.Fail();
            return;
        }

        // Super Admin bypasses ownership checks
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToListAsync();

        if (userRoles.Contains((int)Role.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        // Check if Team Admin is assigned to this team
        var isAssignedToTeam = await _context.UserTeams
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == requirement.TeamId);

        if (isAssignedToTeam)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
