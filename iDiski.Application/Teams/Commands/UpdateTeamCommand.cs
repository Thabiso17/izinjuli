using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Enums;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Teams.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record UpdateTeamCommand(
    Guid    Id,
    string  Name,
    string? LogoUrl,
    int     Founded,
    string? HomeGround,
    string? City,
    string? PrimaryColour,
    string? SecondaryColour
) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Founded)
            .InclusiveBetween(1800, DateTime.UtcNow.Year);

        RuleFor(x => x.PrimaryColour)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Must be a valid hex colour.")
            .When(x => x.PrimaryColour is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTeamCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        UpdateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Team), request.Id);

        // Authorization: Check if user is Super Admin or assigned to this team
        await AuthorizeTeamAccess(request.Id, cancellationToken);

        team.Name            = request.Name;
        team.LogoUrl         = request.LogoUrl;
        team.Founded         = request.Founded;
        team.HomeGround      = request.HomeGround;
        team.City            = request.City;
        team.PrimaryColour   = request.PrimaryColour;
        team.SecondaryColour = request.SecondaryColour;
        team.UpdatedByUserId = _currentUserService.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AuthorizeTeamAccess(Guid teamId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedException();

        // Check if Super Admin
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == (int)Role.SuperAdmin, cancellationToken);

        if (isSuperAdmin)
            return;

        // Check if Team Admin assigned to this team
        var isAssignedToTeam = await _db.UserTeams
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == teamId, cancellationToken);

        if (!isAssignedToTeam)
            throw new ForbiddenException($"User is not authorized to manage team {teamId}");
    }
}
