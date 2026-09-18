using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Authorization;

/// <summary>
/// Whether somebody may run this competition.
///
/// The replacement for DivisionOwnershipHandler, which asked whether a user was assigned to
/// the division a competition belonged to. Competitions belong to no division, so the question
/// is simply whether they were assigned to this one — a competition admin holds a list of the
/// competitions they run, and that list is the whole of their reach.
///
/// Deliberately not "is one of the entrants theirs": a cup drawing clubs from three divisions
/// would otherwise answer to three sets of administrators, none of whom was asked to run it.
/// </summary>
public class CompetitionOwnershipHandler : AuthorizationHandler<CompetitionOwnershipRequirement>
{
    private readonly ILeagueDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompetitionOwnershipHandler(
        ILeagueDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompetitionOwnershipRequirement requirement)
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

        // A SuperAdmin runs everything, including putting right a competition whose own
        // organiser has gone.
        var isSuperAdmin = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == Role.SuperAdmin);

        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // Otherwise: were they given this one? An unassignable competition — Guid.Empty, from
        // a fixture that belongs to nothing — matches nobody, which is the safe direction.
        var assigned = await _context.UserCompetitions
            .AnyAsync(uc => uc.UserId == userId
                            && uc.CompetitionId == requirement.CompetitionId);

        if (assigned)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
