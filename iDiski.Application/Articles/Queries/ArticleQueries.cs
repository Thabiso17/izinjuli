using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Articles.Queries;

// ── Shared DTOs ───────────────────────────────────────────────────────────────

/// <summary>Full article — returned for the detail / reading view.</summary>
public sealed record ArticleDto(
    Guid      Id,
    string    Title,
    string    Slug,
    string    Content,
    string?   Excerpt,
    string?   CoverImageUrl,
    string?   VideoUrl,
    string?   FeaturedImageUrl,
    string    Author,
    bool      IsPublished,
    DateTime? PublishedAt,
    string[]  Tags,
    int       ViewCount,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Lightweight card variant — no Content body.
/// Used for list, tag, and search views to minimise payload.
/// </summary>
public sealed record ArticleSummaryDto(
    Guid      Id,
    string    Title,
    string    Slug,
    string?   Excerpt,
    string?   CoverImageUrl,
    string?   VideoUrl,
    string?   FeaturedImageUrl,
    string    Author,
    DateTime? PublishedAt,
    string[]  Tags
);

// ═════════════════════════════════════════════════════════════════════════════
// GET BY SLUG  —  Angular route: /news/:slug
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetArticleBySlugQuery(string Slug) : IRequest<ArticleDto>;

public sealed class GetArticleBySlugQueryHandler
    : IRequestHandler<GetArticleBySlugQuery, ArticleDto>
{
    private readonly ILeagueDbContext _db;

    public GetArticleBySlugQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<ArticleDto> Handle(
        GetArticleBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles
            .AsNoTracking()
            .Where(a => a.Slug == request.Slug && a.IsPublished)
            .Select(a => new ArticleDto(
                a.Id, a.Title, a.Slug, a.Content, a.Excerpt,
                a.CoverImageUrl, a.VideoUrl, a.FeaturedImageUrl,
                a.Author, a.IsPublished, a.PublishedAt, a.Tags,
                a.ViewCount, a.CreatedAt, a.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(iDiski.Domain.Entities.Article), request.Slug);

        return article;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GET PUBLISHED LIST  —  supports tag filtering for awards content
// ═════════════════════════════════════════════════════════════════════════════

/// <param name="Tag">
/// Optional tag filter. Powers award sections like "Player of the Month" or "Match Reports".
/// Uses PostgreSQL native text[] = ANY(...) — no JSON overhead.
/// Pass null to return all published articles.
/// </param>
public sealed record GetPublishedArticlesQuery(
    string? Tag        = null,
    string? AuthorName = null,
    int     PageNumber = 1,
    int     PageSize   = 10
) : IRequest<PaginatedList<ArticleSummaryDto>>;

public sealed class GetPublishedArticlesQueryHandler
    : IRequestHandler<GetPublishedArticlesQuery, PaginatedList<ArticleSummaryDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPublishedArticlesQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<PaginatedList<ArticleSummaryDto>> Handle(
        GetPublishedArticlesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Articles
            .AsNoTracking()
            .Where(a => a.IsPublished);

        // Tag filter — leverages the native PostgreSQL text[] Contains translation
        if (!string.IsNullOrWhiteSpace(request.Tag))
            query = query.Where(a => a.Tags.Contains(request.Tag.Trim()));

        if (!string.IsNullOrWhiteSpace(request.AuthorName))
            query = query.Where(a => a.Author == request.AuthorName.Trim());

        var projected = query
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new ArticleSummaryDto(
                a.Id, a.Title, a.Slug, a.Excerpt,
                a.CoverImageUrl, a.VideoUrl, a.FeaturedImageUrl,
                a.Author, a.PublishedAt, a.Tags));

        return await PaginatedList<ArticleSummaryDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GET ALL (admin)  —  includes drafts, for the CMS admin panel
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetAllArticlesAdminQuery(
    bool? PublishedOnly = null,
    int   PageNumber    = 1,
    int   PageSize      = 20
) : IRequest<PaginatedList<ArticleSummaryDto>>;

public sealed class GetAllArticlesAdminQueryHandler
    : IRequestHandler<GetAllArticlesAdminQuery, PaginatedList<ArticleSummaryDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAllArticlesAdminQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<PaginatedList<ArticleSummaryDto>> Handle(
        GetAllArticlesAdminQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Articles.AsNoTracking().AsQueryable();

        if (request.PublishedOnly.HasValue)
            query = query.Where(a => a.IsPublished == request.PublishedOnly.Value);

        var projected = query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ArticleSummaryDto(
                a.Id, a.Title, a.Slug, a.Excerpt,
                a.CoverImageUrl, a.VideoUrl, a.FeaturedImageUrl,
                a.Author, a.PublishedAt, a.Tags));

        return await PaginatedList<ArticleSummaryDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
