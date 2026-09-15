using iDiski.Application.Competitions;
using iDiski.Application.Competitions.Commands;
using iDiski.Application.Competitions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

/// <summary>
/// The competitions a division runs: its league, its cups, a sponsor's tournament.
///
/// Reading is open, because these are the public fixtures and tables. Writing requires a
/// division admin, and the pipeline narrows that to the division actually running the
/// competition — a role says what kind of administrator somebody is, never which competitions.
/// </summary>
public sealed class CompetitionsController : BaseApiController
{
    /// <summary>Competitions, optionally narrowed to a division or a season.</summary>
    /// <response code="200">The competitions.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompetitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? divisionId = null,
        [FromQuery] int? season = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetCompetitionsQuery(divisionId, season, isActive), ct));

    /// <summary>One competition.</summary>
    /// <response code="200">The competition.</response>
    /// <response code="404">No competition with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompetitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var competition = await Sender.Send(new GetCompetitionByIdQuery(id), ct);
        return competition is null ? NotFound() : Ok(competition);
    }

    /// <summary>Who is entered, and which of them were invited from another division.</summary>
    /// <response code="200">The entrants.</response>
    [HttpGet("{id:guid}/entrants")]
    [ProducesResponseType(typeof(IReadOnlyList<CompetitionEntrantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntrants(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetCompetitionEntrantsQuery(id), ct));

    /// <summary>Starts a competition in a division.</summary>
    /// <response code="201">Created.</response>
    /// <response code="403">Not an administrator of that division.</response>
    /// <response code="409">The short code is already used in that division and season.</response>
    [HttpPost]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCompetitionCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Renames a competition, or changes its dates. Format is fixed once drawn.</summary>
    /// <response code="204">Updated.</response>
    /// <response code="403">Not an administrator of that division.</response>
    /// <response code="409">Already drawn up, or the short code clashes.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCompetitionCommand command,
        CancellationToken ct)
    {
        if (id != command.CompetitionId)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Deletes a competition that has no fixtures.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="403">Not an administrator of that division.</response>
    /// <response code="409">It has fixtures.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCompetitionCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Enters a club. It may come from another division — that is what a sponsor's cup is for —
    /// but not from one of a different gender, which is refused here rather than at the draw.
    /// </summary>
    /// <response code="204">Entered.</response>
    /// <response code="403">Not an administrator of that division.</response>
    /// <response code="409">Wrong gender, already entered, or already drawn up.</response>
    [HttpPost("{id:guid}/entrants/{teamId:guid}")]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enter(Guid id, Guid teamId, CancellationToken ct)
    {
        await Sender.Send(new EnterTeamCommand(id, teamId), ct);
        return NoContent();
    }

    /// <summary>Withdraws a club that has no fixtures yet.</summary>
    /// <response code="204">Withdrawn.</response>
    /// <response code="403">Not an administrator of that division.</response>
    /// <response code="409">They already have fixtures.</response>
    [HttpDelete("{id:guid}/entrants/{teamId:guid}")]
    [Authorize(Policy = "CanManageDivisions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(Guid id, Guid teamId, CancellationToken ct)
    {
        await Sender.Send(new WithdrawTeamCommand(id, teamId), ct);
        return NoContent();
    }
}
