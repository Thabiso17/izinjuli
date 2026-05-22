using iDiski.Application.Players;
using iDiski.Application.Players.Commands;
using iDiski.Application.Players.Queries;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class PlayersController : BaseApiController
{
    /// <summary>
    /// Returns players, optionally filtered by team and/or active status.
    /// </summary>
    /// <param name="teamId">Filter to a specific team. Omit for all teams.</param>
    /// <param name="activeOnly">Default true — excludes released/inactive players.</param>
    /// <response code="200">List of players.</response>
    /// <response code="404">TeamId supplied but team not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlayerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? teamId,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetPlayersQuery(teamId, activeOnly), ct));

    /// <summary>Gets a single player by ID.</summary>
    /// <response code="200">Player details.</response>
    /// <response code="404">Player not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetPlayerByIdQuery(id), ct));

    /// <summary>Adds a player to a team.</summary>
    /// <response code="201">Player created.</response>
    /// <response code="404">TeamId not found.</response>
    /// <response code="422">Validation failure (e.g. duplicate jersey number).</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlayerCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        // Link back to the team's player list
        return CreatedAtAction(nameof(GetAll), new { teamId = command.TeamId }, id);
    }

    /// <summary>Updates a player's details, team, or status.</summary>
    /// <response code="204">Player updated.</response>
    /// <response code="404">Player not found.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePlayerCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Transfers a player to a different team with a new jersey number.
    /// </summary>
    /// <response code="204">Player transferred successfully.</response>
    /// <response code="404">Player or team not found.</response>
    /// <response code="422">Validation failure (e.g. jersey number already taken).</response>
    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer(
        Guid id,
        [FromBody] TransferPlayerCommand command,
        CancellationToken ct)
    {
        if (id != command.PlayerId)
            return BadRequest("Route ID and body PlayerId do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a player (sets IsActive = false).
    /// Historical stats are preserved.
    /// </summary>
    /// <response code="204">Player deactivated.</response>
    /// <response code="404">Player not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeletePlayerCommand(id), ct);
        return NoContent();
    }
}
