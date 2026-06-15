using FluentValidation;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iDiski.Application.Authentication.Commands;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResponse>;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserLoginDto User
);

public sealed record UserLoginDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? ProfileImageUrl,
    string[] Roles
);

public sealed class LoginCommandValidator : FluentValidation.AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly ILeagueDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        ILeagueDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<LoginCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password");

        // 2. Check if user is active
        if (!user.IsActive)
            throw new UnauthorizedException("User account is disabled");

        // 3. Verify password
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            throw new UnauthorizedException("Invalid email or password");

        // 4. Get user roles
        var roles = user.UserRoles
            .Select(ur => ur.Role.ToString())
            .ToArray();

        // 5. Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user, user.UserRoles.Select(ur => ur.Role).ToArray());

        // 6. Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        // 7. Return response
        return new LoginResponse(
            AccessToken: token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            User: new UserLoginDto(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                ProfileImageUrl: user.ProfileImageUrl,
                Roles: roles
            )
        );
    }
}
