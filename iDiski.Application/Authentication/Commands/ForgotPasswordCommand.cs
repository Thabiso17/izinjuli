using FluentValidation;
using iDiski.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iDiski.Application.Authentication.Commands;

public sealed record ForgotPasswordCommand(
    string Email
) : IRequest<Unit>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly ILeagueDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        ILeagueDbContext db,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // 2. Security: Return success even if user doesn't exist (don't reveal if email exists)
        if (user == null)
        {
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
            return Unit.Value;
        }

        // 3. Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset requested for inactive user: {Email}", request.Email);
            return Unit.Value;
        }

        // 4. Generate secure reset token (32 bytes, base64 encoded)
        var resetToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        // 5. Set token with 1-hour expiry
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _db.SaveChangesAsync(cancellationToken);

        // 6. Send email with reset link
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, cancellationToken);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            throw;
        }

        return Unit.Value;
    }
}
