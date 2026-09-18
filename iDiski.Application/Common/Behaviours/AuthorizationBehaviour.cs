using System.Security.Claims;
using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Common.Behaviours;

/// <summary>
/// Enforces resource-scoped authorization for requests implementing IRequireCompetitionAccess,
/// IRequireTeamAccess, IRequirePlayerAccess or IRequireMatchAccess, by running them through the
/// same CompetitionOwnershipHandler / TeamOwnershipHandler registered for ASP.NET Core
/// authorization.
///
/// [Authorize(Policy = "CanManageCompetitions"/"CanManageTeams")] on a controller action only
/// checks role membership — "is this user a competition admin at all" — and this behaviour is
/// what narrows it to the specific competitions and clubs they were assigned.
///
/// SuperAdmin always passes. A competition admin passes for competitions they were assigned,
/// and for the fixtures inside them. A team admin passes for their own clubs and those clubs'
/// players.
/// </summary>
public sealed class AuthorizationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILeagueDbContext _db;

    public AuthorizationBehaviour(
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILeagueDbContext db)
    {
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _db = db;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequireCompetitionAccess competitionRequest)
        {
            await EnsureAuthorizedAsync(
                new CompetitionOwnershipRequirement(competitionRequest.CompetitionId));
        }

        if (request is IRequireTeamAccess teamRequest)
            await EnsureAuthorizedAsync(new TeamOwnershipRequirement(teamRequest.TeamId));

        if (request is IRequireMatchAccess matchRequest)
        {
            // Recording what happened is part of running the competition the fixture is in,
            // so it reaches whoever runs that. A fixture belonging to no competition resolves
            // to Guid.Empty, which nobody is assigned to — only a SuperAdmin gets through,
            // which is the right direction for a row nobody's scope covers.
            var competitionId = await _db.MatchResults
                .Where(m => m.Id == matchRequest.MatchId)
                .Select(m => m.CompetitionId ?? Guid.Empty)
                .FirstOrDefaultAsync(cancellationToken);

            await EnsureAuthorizedAsync(new CompetitionOwnershipRequirement(competitionId));
        }

        if (request is IRequirePlayerAccess playerRequest)
        {
            var teamId = await _db.Players
                .Where(p => p.Id == playerRequest.PlayerId)
                .Select(p => p.TeamId)
                .FirstOrDefaultAsync(cancellationToken);

            await EnsureAuthorizedAsync(new TeamOwnershipRequirement(teamId));
        }

        return await next();

        async Task EnsureAuthorizedAsync(IAuthorizationRequirement requirement)
        {
            var principal = _currentUserService.User ?? new ClaimsPrincipal();
            var result = await _authorizationService.AuthorizeAsync(principal, resource: null, requirement);

            if (!result.Succeeded)
                throw new ForbiddenException(
                    $"You do not have permission to perform this action ({typeof(TRequest).Name}).");
        }

    }
}
