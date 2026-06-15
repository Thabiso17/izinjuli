using FluentValidation;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iDiski.Application.Authentication.Commands;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Role[] Roles,
    Guid[]? AssignedTeamIds = null,
    Guid[]? AssignedDivisionIds = null
) : IRequest<Guid>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*()_\-+=\[\]{};:'""\\|,.<>?/]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required")
            .Must(roles => roles.Length > 0).WithMessage("Must assign at least one role");

        RuleFor(x => x.AssignedTeamIds)
            .Must((cmd, teamIds) =>
            {
                // If TeamAdmin role is assigned, teams must be provided
                if (cmd.Roles.Contains(Role.TeamAdmin) && (teamIds == null || teamIds.Length == 0))
                    return false;
                return true;
            }).WithMessage("Team Admin role requires at least one assigned team");

        RuleFor(x => x.AssignedDivisionIds)
            .Must((cmd, divisionIds) =>
            {
                // If DivisionAdmin role is assigned, divisions must be provided
                if (cmd.Roles.Contains(Role.DivisionAdmin) && (divisionIds == null || divisionIds.Length == 0))
                    return false;
                return true;
            }).WithMessage("Division Admin role requires at least one assigned division");
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly ILeagueDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        ILeagueDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Only Super Admin can create users
        if (!_currentUserService.IsSuperAdmin)
            throw new ForbiddenException("Only Super Admin can create users");

        // 2. Check email uniqueness
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists");

        // 3. Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Create user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _db.Users.Add(newUser);

        // 5. Assign roles
        var userRoles = request.Roles.Select(role => new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = newUser.Id,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = _currentUserService.UserId
        }).ToList();

        foreach (var userRole in userRoles)
        {
            _db.UserRoles.Add(userRole);
        }

        // 6. Assign teams (for Team Admin)
        if (request.AssignedTeamIds?.Length > 0)
        {
            var userTeams = request.AssignedTeamIds.Select(teamId => new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = _currentUserService.UserId
            }).ToList();

            foreach (var userTeam in userTeams)
            {
                _db.UserTeams.Add(userTeam);
            }
        }

        // 7. Assign divisions (for Division Admin)
        if (request.AssignedDivisionIds?.Length > 0)
        {
            var userDivisions = request.AssignedDivisionIds.Select(divisionId => new UserDivision
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                DivisionId = divisionId,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = _currentUserService.UserId
            }).ToList();

            foreach (var userDivision in userDivisions)
            {
                _db.UserDivisions.Add(userDivision);
            }
        }

        // 8. Save
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {Email} created by {CreatedBy} with roles: {Roles}",
            newUser.Email,
            _currentUserService.Email,
            string.Join(", ", request.Roles)
        );

        return newUser.Id;
    }
}
