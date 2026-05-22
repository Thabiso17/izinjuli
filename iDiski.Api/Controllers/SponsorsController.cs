using iDiski.Application.Sponsors;
using iDiski.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class SponsorsController : BaseApiController
{
    /// <summary>
    /// Returns all sponsors (for admin panel), ordered by DisplayOrder.
    /// </summary>
    /// <response code="200">List of all sponsors.</response>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<SponsorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetAllSponsorsQuery(), ct));

    /// <summary>
    /// Returns active, in-contract sponsors for a given ad placement slot,
    /// ordered by DisplayOrder. Called by the Angular ad rotator on page init.
    /// </summary>
    /// <param name="placement">
    /// Required. One of: Header | Sidebar | Footer | MatchDay | Homepage | NewsPage
    /// </param>
    /// <response code="200">List of active sponsors for the placement.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SponsorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPlacement(
        [FromQuery] AdPlacement placement,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetActiveSponsorsByPlacementQuery(placement), ct));

    /// <summary>Creates a new sponsor/advert entry.</summary>
    /// <response code="201">Sponsor created.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSponsorCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetByPlacement), new { placement = command.Placement }, id);
    }

    /// <summary>Updates an existing sponsor.</summary>
    /// <response code="204">Sponsor updated.</response>
    /// <response code="404">Sponsor not found.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSponsorCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route ID does not match command ID.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Deletes a sponsor.</summary>
    /// <response code="204">Sponsor deleted.</response>
    /// <response code="404">Sponsor not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteSponsorCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Toggles a sponsor's IsActive flag on/off.
    /// Useful for quickly pausing/resuming an advert without deleting it.
    /// </summary>
    /// <response code="204">Toggled successfully.</response>
    /// <response code="404">Sponsor not found.</response>
    [HttpPatch("{id:guid}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ToggleSponsorActiveCommand(id), ct);
        return NoContent();
    }
}
