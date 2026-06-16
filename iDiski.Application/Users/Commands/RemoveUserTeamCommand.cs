using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record RemoveUserTeamCommand(
    Guid UserId,
    Guid TeamId
) : IRequest;

public sealed class RemoveUserTeamCommandValidator : AbstractValidator<RemoveUserTeamCommand>
{
    public RemoveUserTeamCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}

public sealed class RemoveUserTeamCommandHandler : IRequestHandler<RemoveUserTeamCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveUserTeamCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveUserTeamCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can remove team assignments
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can remove team assignments");

        // Find and remove the assignment
        var userTeam = await _db.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == request.UserId && ut.TeamId == request.TeamId, cancellationToken)
            ?? throw new NotFoundException("UserTeam", $"User not assigned to team {request.TeamId}");

        _db.UserTeams.Remove(userTeam);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
