using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record AssignUserTeamCommand(
    Guid UserId,
    Guid TeamId
) : IRequest;

public sealed class AssignUserTeamCommandValidator : AbstractValidator<AssignUserTeamCommand>
{
    public AssignUserTeamCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}

public sealed class AssignUserTeamCommandHandler : IRequestHandler<AssignUserTeamCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AssignUserTeamCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignUserTeamCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can assign teams
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == 3, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can assign teams to users");

        // Verify user exists
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Verify team exists
        var team = await _db.Teams.FindAsync([request.TeamId], cancellationToken)
            ?? throw new NotFoundException("Team", request.TeamId);

        // Check if assignment already exists
        var existingAssignment = await _db.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == request.UserId && ut.TeamId == request.TeamId, cancellationToken);

        if (existingAssignment != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("TeamId", "User already assigned to this team") });

        // Create new assignment
        var userTeam = new iDiski.Domain.Entities.UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TeamId = request.TeamId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserTeams.Add(userTeam);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
