using iDiski.Application.Teams;
using iDiski.Application.Teams.Commands;
using iDiski.Application.Teams.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class TeamsController : BaseApiController
{
    /// <summary>Returns all teams ordered alphabetically, including active player count.</summary>
    /// <response code="200">List of teams.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await Sender.Send(new GetAllTeamsQuery(), ct));

    /// <summary>Returns a single team by ID.</summary>
    /// <response code="200">Team found.</response>
    /// <response code="404">Team not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetTeamByIdQuery(id), ct));

    /// <summary>Creates a new team. Returns the new team's ID in the Location header.</summary>
    /// <response code="201">Team created.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="422">Validation failure (e.g. duplicate ShortCode).</response>
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Updates an existing team. ShortCode is immutable after creation. Requires Team Admin or Super Admin access.</summary>
    /// <response code="204">Team updated.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (must be assigned to this team).</response>
    /// <response code="404">Team not found.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageTeams")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTeamCommand command,
        CancellationToken ct)
    {
        // Ensure route id and body id are consistent
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a team. Fails with 409 if the team has any match history. SuperAdmin only.
    /// </summary>
    /// <response code="204">Team deleted.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">Team not found.</response>
    /// <response code="409">Team has match history and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteTeamCommand(id), ct);
        return NoContent();
    }
}
