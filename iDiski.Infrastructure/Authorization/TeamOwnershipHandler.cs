using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Authorization;

/// <summary>
/// Validates team access through the division hierarchy.
/// - Super Admin: Can access any team
/// - Division Admin: Can access teams within their assigned division(s)
/// - Team Admin: Can access only their explicitly assigned team(s)
/// </summary>
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

        // Super Admin has full access
        var isSuperAdmin = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == Role.SuperAdmin);

        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // Get the team and its division
        var team = await _context.Teams.FindAsync([requirement.TeamId]);
        if (team == null)
        {
            context.Fail();
            return;
        }

        // Check if user is Division Admin assigned to the team's division
        var isDivisionAdmin = await _context.UserDivisions
            .AnyAsync(ud => ud.UserId == userId && ud.DivisionId == team.DivisionId);

        if (isDivisionAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // Check if user is Team Admin assigned to this specific team
        var isTeamAdmin = await _context.UserTeams
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == requirement.TeamId);

        if (isTeamAdmin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
