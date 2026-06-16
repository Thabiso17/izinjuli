using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    bool IsActive
) : IRequest;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can update users
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can update users");

        var user = await _db.Users.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;
        user.UpdatedByUserId = _currentUserService.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
