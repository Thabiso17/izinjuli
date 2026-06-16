using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Enums;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
    private readonly IAuditService _auditService;

    public UpdateTeamCommandHandler(
        ILeagueDbContext db,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _db = db;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task Handle(
        UpdateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Team), request.Id);

        // Authorization: Check if user is Super Admin or assigned to this team
        await AuthorizeTeamAccess(request.Id, cancellationToken);

        // Capture old values for audit log
        var oldValues = JsonSerializer.Serialize(new
        {
            team.Name,
            team.LogoUrl,
            team.Founded,
            team.HomeGround,
            team.City,
            team.PrimaryColour,
            team.SecondaryColour
        });

        // Update team
        team.Name            = request.Name;
        team.LogoUrl         = request.LogoUrl;
        team.Founded         = request.Founded;
        team.HomeGround      = request.HomeGround;
        team.City            = request.City;
        team.PrimaryColour   = request.PrimaryColour;
        team.SecondaryColour = request.SecondaryColour;
        team.UpdatedByUserId = _currentUserService.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        // Log the change
        var newValues = JsonSerializer.Serialize(new
        {
            team.Name,
            team.LogoUrl,
            team.Founded,
            team.HomeGround,
            team.City,
            team.PrimaryColour,
            team.SecondaryColour
        });

        await _auditService.LogAsync(
            "Team",
            team.Id,
            "Updated",
            $"Updated team: {team.Name}",
            oldValues,
            newValues,
            cancellationToken);
    }

    private async Task AuthorizeTeamAccess(Guid teamId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedException("User must be authenticated");

        // Super Admin has full access
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == Role.SuperAdmin, cancellationToken);

        if (isSuperAdmin)
            return;

        // Get team and its division
        var team = await _db.Teams.FindAsync([teamId], cancellationToken);
        if (team == null)
            throw new NotFoundException(nameof(Domain.Entities.Team), teamId);

        // Division Admin can manage teams in their division
        var isDivisionAdmin = await _db.UserDivisions
            .AnyAsync(ud => ud.UserId == userId && ud.DivisionId == team.DivisionId, cancellationToken);

        if (isDivisionAdmin)
            return;

        // Team Admin can only manage their assigned team
        var isTeamAdmin = await _db.UserTeams
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == teamId, cancellationToken);

        if (!isTeamAdmin)
            throw new ForbiddenException($"User is not authorized to manage team {teamId}");
    }
}
