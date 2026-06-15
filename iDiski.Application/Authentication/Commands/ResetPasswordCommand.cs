using FluentValidation;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iDiski.Application.Authentication.Commands;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Unit>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*()_\-+=\[\]{};:'""\\|,.<>?/]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly ILeagueDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        ILeagueDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by reset token
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired password reset token");

        // 2. Check if token has expired (1 hour expiry)
        if (user.PasswordResetTokenExpiry == null || DateTime.UtcNow > user.PasswordResetTokenExpiry)
            throw new UnauthorizedException("Password reset token has expired");

        // 3. Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // 4. Update password and clear reset token
        user.PasswordHash = newPasswordHash;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);

        return Unit.Value;
    }
}
