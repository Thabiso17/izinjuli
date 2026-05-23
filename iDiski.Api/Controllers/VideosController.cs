using iDiski.Application.Videos;
using iDiski.Application.Videos.Commands;
using iDiski.Application.Videos.Queries;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

public sealed class VideosController : BaseApiController
{
    /// <summary>
    /// Returns published videos for the homepage (pinned first, then by date).
    /// </summary>
    /// <param name="limit">Maximum number of videos to return (default 10).</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<VideoSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        => Ok(await Sender.Send(new GetPublishedVideosQuery(limit), ct));

    /// <summary>
    /// Returns all videos for admin panel.
    /// </summary>
    /// <param name="publishedOnly">Filter by published status.</param>
    [HttpGet("admin")]
    [ProducesResponseType(typeof(List<VideoSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAdmin(
        [FromQuery] bool? publishedOnly = null,
        CancellationToken ct = default)
        => Ok(await Sender.Send(new GetAllVideosAdminQuery(publishedOnly), ct));

    /// <summary>
    /// Gets a single video by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await Sender.Send(new GetVideoByIdQuery(id), ct));

    /// <summary>
    /// Creates a new video.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVideoCommand command,
        CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Updates an existing video.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVideoCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route ID and body ID do not match.");

        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Publishes a video.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await Sender.Send(new PublishVideoCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Unpublishes a video.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await Sender.Send(new UnpublishVideoCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a video.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteVideoCommand(id), ct);
        return NoContent();
    }
}
