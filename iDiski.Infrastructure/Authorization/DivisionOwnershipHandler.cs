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

        // Super Admin has full access
        var isSuperAdmin = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == (int)Role.SuperAdmin);

        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // Division Admin must be explicitly assigned to this division
        var isDivisionAdmin = await _context.UserDivisions
            .AnyAsync(ud => ud.UserId == userId && ud.DivisionId == requirement.DivisionId);

        if (isDivisionAdmin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
