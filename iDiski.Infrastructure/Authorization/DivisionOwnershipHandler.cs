using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Authorization;

public class DivisionOwnershipHandler : AuthorizationHandler<DivisionOwnershipRequirement>
{
    private readonly ILeagueDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DivisionOwnershipHandler(ILeagueDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DivisionOwnershipRequirement requirement)
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

        // Check if Division Admin is assigned to this division
        var isAssignedToDivision = await _context.UserDivisions
            .AnyAsync(ud => ud.UserId == userId && ud.DivisionId == requirement.DivisionId);

        if (isAssignedToDivision)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
