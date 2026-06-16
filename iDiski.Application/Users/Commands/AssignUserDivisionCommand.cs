using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record AssignUserDivisionCommand(
    Guid UserId,
    Guid DivisionId
) : IRequest;

public sealed class AssignUserDivisionCommandValidator : AbstractValidator<AssignUserDivisionCommand>
{
    public AssignUserDivisionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DivisionId).NotEmpty();
    }
}

public sealed class AssignUserDivisionCommandHandler : IRequestHandler<AssignUserDivisionCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AssignUserDivisionCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignUserDivisionCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can assign divisions
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == 3, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can assign divisions to users");

        // Verify user exists
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Verify division exists
        var division = await _db.Divisions.FindAsync([request.DivisionId], cancellationToken)
            ?? throw new NotFoundException("Division", request.DivisionId);

        // Check if assignment already exists
        var existingAssignment = await _db.UserDivisions
            .FirstOrDefaultAsync(ud => ud.UserId == request.UserId && ud.DivisionId == request.DivisionId, cancellationToken);

        if (existingAssignment != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("DivisionId", "User already assigned to this division") });

        // Create new assignment
        var userDivision = new iDiski.Domain.Entities.UserDivision
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DivisionId = request.DivisionId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserDivisions.Add(userDivision);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
