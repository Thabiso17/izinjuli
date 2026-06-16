using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record RemoveUserRoleCommand(
    Guid UserId,
    int Role
) : IRequest;

public sealed class RemoveUserRoleCommandValidator : AbstractValidator<RemoveUserRoleCommand>
{
    public RemoveUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).InclusiveBetween(1, 3).WithMessage("Role must be 1 (TeamAdmin), 2 (DivisionAdmin), or 3 (SuperAdmin)");
    }
}

public sealed class RemoveUserRoleCommandHandler : IRequestHandler<RemoveUserRoleCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveUserRoleCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveUserRoleCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can remove roles
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == 3, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can remove roles");

        // Find and remove the role assignment
        var userRole = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.Role == request.Role, cancellationToken)
            ?? throw new NotFoundException("UserRole", $"User does not have role {request.Role}");

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
