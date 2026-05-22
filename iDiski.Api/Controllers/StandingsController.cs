using iDiski.Application.Standings;
using iDiski.Application.Standings.Queries;
using iDiski.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace iDiski.Api.Controllers;

public sealed class StandingsController : BaseApiController
{
    /// <summary>
    /// Returns the full league table for a given season, computed from all
    /// completed match results. Teams with no matches played appear at the bottom
    /// on 0 points so the Angular table always shows all registered clubs.
    /// </summary>
    /// <param name="season">Required. Season year, e.g. 2025.</param>
    /// <param name="divisionId">Optional. Filter by division ID.</param>
    /// <param name="upToMatchweek">
    /// Optional. Returns a historical snapshot of the table as it stood after
    /// this matchweek — useful for "Matchweek 10 rewind" features in your Angular app.
    /// </param>
    /// <response code="200">League table ordered by Points → GD → GF.</response>
    [HttpGet("table")]
    [ProducesResponseType(typeof(LeagueTableDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTable(
        [FromQuery] int   season,
        [FromQuery] Guid? divisionId = null,
        [FromQuery] int?  upToMatchweek = null,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetLeagueStandingsQuery(season, divisionId, upToMatchweek), ct));

    /// <summary>
    /// Returns the top scorers leaderboard for a season.
    /// Sorted by Goals → Assists → Last Name.
    /// </summary>
    /// <param name="season">Season year.</param>
    /// <param name="topN">Number of players to return. Default 10.</param>
    /// <response code="200">Ordered list of top scorers.</response>
    [HttpGet("top-scorers")]
    [ProducesResponseType(typeof(IReadOnlyList<TopScorerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopScorers(
        [FromQuery] int season,
        [FromQuery] int topN = 10,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetTopScorersQuery(season, topN), ct));

    /// <summary>
    /// Returns head-to-head statistics between two clubs — all-time meetings,
    /// win/draw/loss record, goals, and the 10 most recent encounters.
    ///
    /// Example: GET /api/standings/head-to-head?teamAId=...&amp;teamBId=...
    /// </summary>
    /// <param name="teamAId">First team's ID.</param>
    /// <param name="teamBId">Second team's ID.</param>
    /// <response code="200">Head-to-head summary.</response>
    /// <response code="404">One or both teams not found.</response>
    [HttpGet("head-to-head")]
    [ProducesResponseType(typeof(HeadToHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHeadToHead(
        [FromQuery] Guid teamAId,
        [FromQuery] Guid teamBId,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetHeadToHeadQuery(teamAId, teamBId), ct));
}
