using iDiski.Application.Authentication.Commands;
using iDiski.Application.Authentication.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : BaseApiController
{
    /// <summary>
    /// Login with email and password. Returns JWT token and user info.
    /// POST /api/auth/login
    /// Response: 200 Ok with { accessToken, expiresAt, user }
    ///           401 Unauthorized if invalid credentials
    ///           422 Validation error
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
        => Ok(await Sender.Send(command));

    /// <summary>
    /// Get current authenticated user's profile.
    /// GET /api/auth/me
    /// Auth: Requires valid JWT token in Authorization header
    /// Response: 200 Ok with current user profile
    ///           401 Unauthorized if not authenticated
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
        => Ok(await Sender.Send(new GetCurrentUserQuery()));

    /// <summary>
    /// Request password reset email. Public endpoint (no auth required).
    /// POST /api/auth/forgot-password
    /// Body: { email }
    /// Response: Always 200 Ok for security (doesn't reveal if email exists)
    ///           Sends email with reset link if user found and active
    ///           422 Validation error
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await Sender.Send(command);
        return Ok(new { message = "If the email exists, a password reset link has been sent." });
    }

    /// <summary>
    /// Reset password using token from email. Public endpoint.
    /// POST /api/auth/reset-password
    /// Body: { token, newPassword, confirmPassword }
    /// Token valid for 1 hour after ForgotPassword request.
    /// Response: 200 Ok on success
    ///           401 Unauthorized if token invalid or expired
    ///           422 Validation error
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await Sender.Send(command);
        return Ok(new { message = "Password reset successfully. You can now login with your new password." });
    }

    /// <summary>
    /// Create a new user account. Super Admin only.
    /// POST /api/auth/create-user
    /// Auth: Requires authenticated user with SuperAdmin role
    /// Body: { email, password, firstName, lastName, roles[], assignedTeamIds?, assignedDivisionIds? }
    /// Response: 201 Created with userId
    ///           403 Forbidden if not Super Admin
    ///           409 Conflict if email already exists
    ///           422 Validation error
    /// </summary>
    [HttpPost("create-user")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var userId = await Sender.Send(command);
        return CreatedAtAction(nameof(GetCurrentUser), new { id = userId }, new { userId });
    }
}
