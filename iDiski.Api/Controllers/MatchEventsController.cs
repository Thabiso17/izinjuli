using iDiski.Application.MatchEvents.Commands;
using iDiski.Application.MatchEvents.Queries;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchEventsController : BaseApiController
{
    /// <summary>
    /// Get all events for a specific match
    /// </summary>
    [HttpGet("match/{matchId:guid}")]
    public async Task<IActionResult> GetByMatch(Guid matchId)
    {
        var query = new GetMatchEventsQuery(matchId);
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all events for a specific player
    /// </summary>
    [HttpGet("player/{playerId:guid}")]
    public async Task<IActionResult> GetByPlayer(Guid playerId, [FromQuery] int? season)
    {
        var query = new GetPlayerMatchEventsQuery(playerId, season);
        var result = await Sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Record match events (goals, assists, cards, etc.)
    /// Replaces any existing events for the match
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RecordEvents([FromBody] RecordMatchEventsCommand command)
    {
        await Sender.Send(command);
        return NoContent();
    }
}
