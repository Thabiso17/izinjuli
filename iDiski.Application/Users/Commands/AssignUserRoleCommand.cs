using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record AssignUserRoleCommand(
    Guid UserId,
    int Role
) : IRequest;

public sealed class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).InclusiveBetween(1, 3).WithMessage("Role must be 1 (TeamAdmin), 2 (DivisionAdmin), or 3 (SuperAdmin)");
    }
}

public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AssignUserRoleCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can assign roles
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can assign roles");

        // Verify user exists
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Check if role already assigned
        var existingRole = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.Role == (iDiski.Domain.Enums.Role)request.Role, cancellationToken);

        if (existingRole != null)
            throw new iDiski.Application.Common.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Role", "User already has this role") });

        // Create new role assignment
        var userRole = new iDiski.Domain.Entities.UserRole
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Role = (iDiski.Domain.Enums.Role)request.Role,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
