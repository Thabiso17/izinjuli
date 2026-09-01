using iDiski.Application.DataManagement.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

/// <summary>
/// Bulk data-wipe operations for clearing test/seed data before launch. SuperAdmin only.
/// </summary>
[Route("api/clear-data")]
[Authorize(Policy = "SuperAdminOnly")]
public sealed class ClearDataController : BaseApiController
{
    /// <summary>Removes every player on a team (players, suspensions, match events). Returns the number of players removed.</summary>
    /// <response code="200">Players removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">Team not found.</response>
    [HttpDelete("players/team/{teamId:guid}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearPlayers(Guid teamId, CancellationToken ct) =>
        Ok(await Sender.Send(new ClearPlayersCommand(teamId), ct));

    /// <summary>Permanently removes a team and all of its players, suspensions, match events and match history. Returns 1 on success.</summary>
    /// <response code="200">Team removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">Team not found.</response>
    [HttpDelete("team/{teamId:guid}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearTeam(Guid teamId, CancellationToken ct) =>
        Ok(await Sender.Send(new ClearTeamCommand(teamId), ct));

    /// <summary>Permanently removes a division and everything nested under it: teams, players, suspensions, match events and match history. Returns the number of teams removed.</summary>
    /// <response code="200">Division removed.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">Division not found.</response>
    [HttpDelete("division/{divisionId:guid}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearDivision(Guid divisionId, CancellationToken ct) =>
        Ok(await Sender.Send(new ClearDivisionCommand(divisionId), ct));
}
