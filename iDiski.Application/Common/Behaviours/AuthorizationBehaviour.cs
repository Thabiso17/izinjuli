using System.Security.Claims;
using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Common.Behaviours;

/// <summary>
/// Enforces resource-scoped authorization for requests implementing IRequireDivisionAccess,
/// IRequireTeamAccess, IRequirePlayerAccess or IRequireMatchAccess, by running them through
/// the same
/// TeamOwnershipHandler / DivisionOwnershipHandler registered for ASP.NET Core authorization.
/// [Authorize(Policy = "CanManageTeams"/"CanManageDivisions")] on a controller action only
/// checks role membership (e.g. "is this user a DivisionAdmin at all") — this behaviour is
/// what scopes access down to the specific division/team/player the requester is assigned to.
/// SuperAdmin always passes; a DivisionAdmin passes for teams/players in their division;
/// a TeamAdmin passes only for their own team/players.
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
        if (request is IRequireDivisionAccess divisionRequest)
            await EnsureAuthorizedAsync(new DivisionOwnershipRequirement(divisionRequest.DivisionId));

        if (request is IRequireTeamAccess teamRequest)
            await EnsureAuthorizedAsync(new TeamOwnershipRequirement(teamRequest.TeamId));

        if (request is IRequireMatchAccess matchRequest)
        {
            // A fixture has no division: it belongs to a competition, which may be contested
            // across several. So its scope is the two clubs playing — whoever administers
            // either side's division may record what happened between them.
            var clubDivisions = await _db.MatchResults
                .Where(m => m.Id == matchRequest.MatchId)
                .Select(m => new
                {
                    Home = m.HomeTeam != null ? m.HomeTeam.DivisionId : (Guid?)null,
                    Away = m.AwayTeam != null ? m.AwayTeam.DivisionId : (Guid?)null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            // An empty bracket slot has no clubs and so nobody's scope covers it, leaving it
            // to a SuperAdmin. Failing closed is the right direction, and there is nothing to
            // record on a semi-final whose finalists have not arrived.
            var candidates = new List<Guid>();

            if (clubDivisions is not null)
            {
                if (clubDivisions.Home is Guid home) candidates.Add(home);
                if (clubDivisions.Away is Guid away && away != clubDivisions.Home)
                    candidates.Add(away);
            }

            await EnsureAnyAuthorizedAsync(
                candidates.Select(id => new DivisionOwnershipRequirement(id)).ToList());
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

        // Passes if the requester satisfies any one of these — a fixture is reachable by
        // either club's administrator.
        async Task EnsureAnyAuthorizedAsync(IReadOnlyList<IAuthorizationRequirement> options)
        {
            var principal = _currentUserService.User ?? new ClaimsPrincipal();

            foreach (var requirement in options)
            {
                var result = await _authorizationService.AuthorizeAsync(
                    principal, resource: null, requirement);

                if (result.Succeeded) return;
            }

            // A SuperAdmin passes any DivisionOwnershipRequirement, so give an unscopeable
            // fixture one to pass rather than refusing them along with everybody else.
            if (options.Count == 0)
            {
                await EnsureAuthorizedAsync(new DivisionOwnershipRequirement(Guid.Empty));
                return;
            }

            throw new ForbiddenException(
                $"You do not have permission to perform this action ({typeof(TRequest).Name}).");
        }
    }
}
