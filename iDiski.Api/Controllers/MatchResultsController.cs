using iDiski.Application.Common.Models;
using iDiski.Application.MatchResults;
using iDiski.Application.Matches.Commands;
using iDiski.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class MatchResultsController : BaseApiController
{
    /// <summary>
    /// Returns a paginated fixture/results list for a season.
    /// Filter further by matchweek, team, or status.
    /// </summary>
    /// <param name="season">Required. The league season year, e.g. 2025.</param>
    /// <param name="matchweek">Optional matchweek number.</param>
    /// <param name="teamId">Optional — returns only matches involving this team.</param>
    /// <param name="status">Optional status filter: Scheduled | InProgress | Completed | Postponed | Cancelled</param>
    /// <param name="pageNumber">Default 1.</param>
    /// <param name="pageSize">Default 20, max recommended 50.</param>
    /// <response code="200">Paginated fixtures list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<MatchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFixtures(
        [FromQuery] int season,
        [FromQuery] int? matchweek       = null,
        [FromQuery] Guid? teamId         = null,
        [FromQuery] MatchStatus? status  = null,
        [FromQuery] int pageNumber       = 1,
        [FromQuery] int pageSize         = 20,
        CancellationToken ct             = default) =>
        Ok(await Sender.Send(
            new GetFixturesQuery(season, matchweek, teamId, status, pageNumber, pageSize), ct));

    /// <summary>Returns a single match by ID.</summary>
    /// <response code="200">Match found.</response>
    /// <response code="404">Match not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetMatchByIdQuery(id), ct));

    /// <summary>Schedules a new match fixture (Division Admin required for the division).</summary>
    /// <response code="201">Match created.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (must be admin for the division).</response>
    /// <response code="422">Validation failure (e.g. same home and away team).</response>
    [HttpPost]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMatchResultCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Submits or updates the score and status of a match (Division Admin required).
    /// Use this endpoint to record final results, flag postponements, etc.
    /// </summary>
    /// <response code="204">Score updated.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (must be admin for the division).</response>
    /// <response code="404">Match not found.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("{id:guid}/score")]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateScore(
        Guid id,
        [FromBody] UpdateMatchScoreCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Generates fixtures for a division using round-robin algorithm (SuperAdmin only).
    /// </summary>
    /// <param name="command">Generation parameters (division, season, home-and-away, start date)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Fixtures generated successfully.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    /// <response code="404">Division not found.</response>
    /// <response code="422">Validation failure (e.g., not enough teams).</response>
    [HttpPost("generate")]
    [Authorize(Policy = "SuperAdminOnly")]
    [ProducesResponseType(typeof(GenerateFixturesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateFixtures(
        [FromBody] GenerateFixturesCommand command,
        CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return Ok(result);
    }
}
