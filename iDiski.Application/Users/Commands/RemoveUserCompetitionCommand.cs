using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record RemoveUserCompetitionCommand(
    Guid UserId,
    Guid CompetitionId
) : IRequest;

public sealed class RemoveUserCompetitionCommandValidator : AbstractValidator<RemoveUserCompetitionCommand>
{
    public RemoveUserCompetitionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompetitionId).NotEmpty();
    }
}

public sealed class RemoveUserCompetitionCommandHandler : IRequestHandler<RemoveUserCompetitionCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveUserCompetitionCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveUserCompetitionCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can remove competition assignments
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can remove competition assignments");

        // Find and remove the assignment
        var userCompetition = await _db.UserCompetitions
            .FirstOrDefaultAsync(ud => ud.UserId == request.UserId && ud.CompetitionId == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException("UserCompetition", $"User not assigned to competition {request.CompetitionId}");

        _db.UserCompetitions.Remove(userCompetition);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
