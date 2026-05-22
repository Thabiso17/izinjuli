using iDiski.Application.Suspensions.Commands;
using iDiski.Application.Suspensions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuspensionsController : BaseApiController
{
    /// <summary>
    /// Get all active suspensions with optional division filter
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] Guid? divisionId)
    {
        var query = new GetActiveSuspensionsQuery(divisionId);
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get suspension history for a specific player
    /// </summary>
    [HttpGet("player/{playerId:guid}")]
    public async Task<IActionResult> GetPlayerHistory(Guid playerId)
    {
        var query = new GetPlayerSuspensionHistoryQuery(playerId);
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Manually create a suspension for a player
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSuspensionCommand command)
    {
        var suspensionId = await Sender.Send(command);
        return CreatedAtAction(nameof(GetPlayerHistory), new { playerId = command.PlayerId }, suspensionId);
    }
}
