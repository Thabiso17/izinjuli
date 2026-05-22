using iDiski.Application.PageLayoutConfigs;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class PageLayoutConfigsController : BaseApiController
{
    /// <summary>
    /// Returns the ordered, visible component list for a given page.
    /// The Angular app calls this on init and renders components in DisplayOrder sequence.
    ///
    /// Example: GET /api/pagelayoutconfigs?pageName=main
    /// Returns: [{ componentName: "HeroBanner", displayOrder: 1, isVisible: true, configJson: null }, ...]
    /// </summary>
    /// <param name="pageName">
    /// Page identifier: "main" | "matches" | "news" | "teams" — must match Angular route keys.
    /// </param>
    /// <response code="200">Ordered component layout for the page.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PageLayoutConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLayout(
        [FromQuery] string pageName,
        CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetPageLayoutQuery(pageName), ct));

    /// <summary>
    /// Upserts a single component's layout config (create or update by PageName + ComponentName).
    /// Call this from a per-component settings panel in the Angular admin UI.
    /// </summary>
    /// <response code="200">Component layout upserted. Returns the entity ID.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertPageLayoutCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return Ok(id);
    }

    /// <summary>
    /// Replaces the entire layout for a page in one call.
    /// Use this after a drag-and-drop reorder in the Angular admin panel to persist
    /// the new DisplayOrder for all components at once.
    ///
    /// Example body:
    /// {
    ///   "pageName": "main",
    ///   "modifiedByUser": "admin",
    ///   "components": [
    ///     { "componentName": "HeroBanner",   "displayOrder": 1, "isVisible": true  },
    ///     { "componentName": "LeagueTable",  "displayOrder": 2, "isVisible": true  },
    ///     { "componentName": "SponsorBanner","displayOrder": 3, "isVisible": false }
    ///   ]
    /// }
    /// </summary>
    /// <response code="204">Page layout updated.</response>
    /// <response code="422">Validation failure.</response>
    [HttpPut("bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BulkUpdate(
        [FromBody] BulkUpdatePageLayoutCommand command,
        CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return NoContent();
    }
}
