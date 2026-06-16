using iDiski.Application.Users;
using iDiski.Application.Users.Queries;
using iDiski.Application.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class UsersController : BaseApiController
{
    /// <summary>
    /// Get all users (SuperAdmin only).
    /// </summary>
    /// <response code="200">List of all users.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await Sender.Send(new GetAllUsersQuery(), ct));

    /// <summary>
    /// Get a specific user by ID (SuperAdmin only).
    /// Includes roles and team/division assignments.
    /// </summary>
    /// <response code="200">User found with details.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await Sender.Send(new GetUserByIdQuery(id), ct);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Update user basic info (FirstName, LastName, IsActive). SuperAdmin only.
    /// </summary>
    /// <response code="204">User updated.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User not found.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Assign a role to a user (SuperAdmin only).
    /// Role codes: 1=TeamAdmin, 2=DivisionAdmin, 3=SuperAdmin.
    /// </summary>
    /// <response code="204">Role assigned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User not found.</response>
    /// <response code="422">User already has this role or invalid role.</response>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignUserRoleCommand command, CancellationToken ct)
    {
        if (id != command.UserId)
            return BadRequest("Route ID and body UserId do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Remove a role from a user (SuperAdmin only).
    /// </summary>
    /// <response code="204">Role removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User or role assignment not found.</response>
    [HttpDelete("{id:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, int role, CancellationToken ct)
    {
        await Sender.Send(new RemoveUserRoleCommand(id, role), ct);
        return NoContent();
    }

    /// <summary>
    /// Assign a team to a user (SuperAdmin only, typically for TeamAdmin role).
    /// </summary>
    /// <response code="204">Team assigned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User or team not found.</response>
    /// <response code="422">User already assigned to this team.</response>
    [HttpPost("{id:guid}/teams")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignTeam(Guid id, [FromBody] AssignUserTeamCommand command, CancellationToken ct)
    {
        if (id != command.UserId)
            return BadRequest("Route ID and body UserId do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Remove a team assignment from a user (SuperAdmin only).
    /// </summary>
    /// <response code="204">Team assignment removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User or team assignment not found.</response>
    [HttpDelete("{id:guid}/teams/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeam(Guid id, Guid teamId, CancellationToken ct)
    {
        await Sender.Send(new RemoveUserTeamCommand(id, teamId), ct);
        return NoContent();
    }

    /// <summary>
    /// Assign a division to a user (SuperAdmin only, typically for DivisionAdmin role).
    /// </summary>
    /// <response code="204">Division assigned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User or division not found.</response>
    /// <response code="422">User already assigned to this division.</response>
    [HttpPost("{id:guid}/divisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignDivision(Guid id, [FromBody] AssignUserDivisionCommand command, CancellationToken ct)
    {
        if (id != command.UserId)
            return BadRequest("Route ID and body UserId do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Remove a division assignment from a user (SuperAdmin only).
    /// </summary>
    /// <response code="204">Division assignment removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">User or division assignment not found.</response>
    [HttpDelete("{id:guid}/divisions/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDivision(Guid id, Guid divisionId, CancellationToken ct)
    {
        await Sender.Send(new RemoveUserDivisionCommand(id, divisionId), ct);
        return NoContent();
    }
}
