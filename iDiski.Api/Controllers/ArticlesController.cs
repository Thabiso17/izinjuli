using iDiski.Application.Articles.Commands;
using iDiski.Application.Articles.Queries;
using iDiski.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using PublishArticleCommand = iDiski.Application.Articles.PublishArticleCommand;

namespace iDiski.Api.Controllers;

public sealed class ArticlesController : BaseApiController
{
    // ── PUBLIC ENDPOINTS ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of published articles, newest first.
    /// Filter by tag to power award sections, e.g. tag=Player+of+the+Month.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ArticleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished(
        [FromQuery] string? tag        = null,
        [FromQuery] string? authorName = null,
        [FromQuery] int pageNumber     = 1,
        [FromQuery] int pageSize       = 10,
        CancellationToken ct           = default) =>
        Ok(await Sender.Send(
            new GetPublishedArticlesQuery(tag, authorName, pageNumber, pageSize), ct));

    /// <summary>
    /// Returns a single published article by its URL slug.
    /// Angular routing calls this on /news/:slug.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await Sender.Send(new GetArticleBySlugQuery(slug), ct));

    // ── ADMIN ENDPOINTS ───────────────────────────────────────────────────────

    /// <summary>[Admin] Returns all articles including drafts, newest first.</summary>
    [HttpGet("admin")]
    [ProducesResponseType(typeof(PaginatedList<ArticleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAdmin(
        [FromQuery] bool? publishedOnly = null,
        [FromQuery] int pageNumber      = 1,
        [FromQuery] int pageSize        = 20,
        CancellationToken ct            = default) =>
        Ok(await Sender.Send(
            new GetAllArticlesAdminQuery(publishedOnly, pageNumber, pageSize), ct));

    /// <summary>
    /// Creates a new article. The SEO slug is auto-generated from Title.
    /// Returns both the new ID and the generated slug.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateArticleResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateArticleCommand command,
        CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>Updates content. Slug is immutable after creation.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateArticleCommand command,
        CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route ID and body ID do not match.");
        await Sender.Send(command, ct);
        return NoContent();
    }

    /// <summary>Publishes a draft article.</summary>
    [HttpPatch("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await Sender.Send(new PublishArticleCommand(id), ct);
        return NoContent();
    }

    /// <summary>Retracts a published article back to draft. Required before deletion.</summary>
    [HttpPatch("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await Sender.Send(new UnpublishArticleCommand(id), ct);
        return NoContent();
    }

    /// <summary>Deletes an article. Only drafts may be deleted — unpublish first.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteArticleCommand(id), ct);
        return NoContent();
    }
}
