using iDiski.Application.Divisions;
using iDiski.Application.Divisions.Commands;
using iDiski.Application.Divisions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DivisionsController : BaseApiController
{
    /// <summary>
    /// Get all available seasons from the database
    /// </summary>
    [HttpGet("seasons")]
    public async Task<IActionResult> GetSeasons()
    {
        var query = new GetAvailableSeasonsQuery();
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all divisions with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? season, [FromQuery] bool? isActive)
    {
        var query = new GetDivisionsQuery(season, isActive);
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a single division by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetDivisionByIdQuery(id);
        var result = await Sender.Send(query);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Create a new division (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateDivisionCommand command)
    {
        var divisionId = await Sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = divisionId }, divisionId);
    }

    /// <summary>
    /// Update an existing division. Requires Division Admin (assigned to division) or SuperAdmin.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageDivisions")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDivisionCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        await Sender.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Delete a division (SuperAdmin only, only if no teams or matches assigned)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteDivisionCommand(id);
        await Sender.Send(command);
        return NoContent();
    }
}
