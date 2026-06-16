using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record RemoveUserDivisionCommand(
    Guid UserId,
    Guid DivisionId
) : IRequest;

public sealed class RemoveUserDivisionCommandValidator : AbstractValidator<RemoveUserDivisionCommand>
{
    public RemoveUserDivisionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DivisionId).NotEmpty();
    }
}

public sealed class RemoveUserDivisionCommandHandler : IRequestHandler<RemoveUserDivisionCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveUserDivisionCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveUserDivisionCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can remove division assignments
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can remove division assignments");

        // Find and remove the assignment
        var userDivision = await _db.UserDivisions
            .FirstOrDefaultAsync(ud => ud.UserId == request.UserId && ud.DivisionId == request.DivisionId, cancellationToken)
            ?? throw new NotFoundException("UserDivision", $"User not assigned to division {request.DivisionId}");

        _db.UserDivisions.Remove(userDivision);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
