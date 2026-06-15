using iDiski.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace iDiski.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Email");
            var resetUrl = $"https://izinjuli.vercel.app/reset-password?token={Uri.EscapeDataString(resetToken)}";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("iDiski", smtpSettings["From"]));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = "Reset Your Password";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <h2>Password Reset Request</h2>
                <p>Click the link below to reset your password. This link is valid for 1 hour.</p>
                <p><a href='{resetUrl}'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpSettings["SmtpHost"],
                int.Parse(smtpSettings["SmtpPort"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls,
                cancellationToken);

            await client.AuthenticateAsync(
                smtpSettings["SmtpUser"],
                smtpSettings["SmtpPassword"],
                cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw;
        }
    }
}
