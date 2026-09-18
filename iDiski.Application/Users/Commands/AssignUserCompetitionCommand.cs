using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Users.Commands;

public sealed record AssignUserCompetitionCommand(
    Guid UserId,
    Guid CompetitionId
) : IRequest;

public sealed class AssignUserCompetitionCommandValidator : AbstractValidator<AssignUserCompetitionCommand>
{
    public AssignUserCompetitionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompetitionId).NotEmpty();
    }
}

public sealed class AssignUserCompetitionCommandHandler : IRequestHandler<AssignUserCompetitionCommand>
{
    private readonly ILeagueDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AssignUserCompetitionCommandHandler(ILeagueDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignUserCompetitionCommand request, CancellationToken cancellationToken)
    {
        // Only Super Admin can assign competitions
        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == _currentUserService.UserId && ur.Role == iDiski.Domain.Enums.Role.SuperAdmin, cancellationToken);

        if (!isSuperAdmin)
            throw new ForbiddenException("Only Super Admin can assign competitions to users");

        // Verify user exists
        var user = await _db.Users.FindAsync([request.UserId], cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Verify competition exists
        var competition = await _db.Competitions.FindAsync([request.CompetitionId], cancellationToken)
            ?? throw new NotFoundException("Competition", request.CompetitionId);

        // Check if assignment already exists
        var existingAssignment = await _db.UserCompetitions
            .FirstOrDefaultAsync(ud => ud.UserId == request.UserId && ud.CompetitionId == request.CompetitionId, cancellationToken);

        if (existingAssignment != null)
            throw new iDiski.Application.Common.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("CompetitionId", "User already assigned to this competition") });

        // Create new assignment
        var userCompetition = new iDiski.Domain.Entities.UserCompetition
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CompetitionId = request.CompetitionId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserCompetitions.Add(userCompetition);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
