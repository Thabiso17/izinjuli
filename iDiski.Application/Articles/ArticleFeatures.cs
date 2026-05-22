using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Models;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Articles;

// ── DTO ───────────────────────────────────────────────────────────────────────

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
    DateTime  CreatedAt
);

/// <summary>Lightweight card version — omits full Content for list views.</summary>
public sealed record ArticleSummaryDto(
    Guid      Id,
    string    Title,
    string    Slug,
    string?   Excerpt,
    string?   CoverImageUrl,
    string?   FeaturedImageUrl,
    string    Author,
    DateTime? PublishedAt,
    string[]  Tags
);

// ═════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═════════════════════════════════════════════════════════════════════════════

public sealed record GetPublishedArticlesQuery(
    string? Tag        = null,
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

        if (!string.IsNullOrWhiteSpace(request.Tag))
            query = query.Where(a => a.Tags.Contains(request.Tag));

        var projected = query
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new ArticleSummaryDto(
                a.Id, a.Title, a.Slug, a.Excerpt,
                a.CoverImageUrl, a.FeaturedImageUrl,
                a.Author, a.PublishedAt, a.Tags));

        return await PaginatedList<ArticleSummaryDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

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
                a.ViewCount, a.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return article ?? throw new NotFoundException(nameof(Article), request.Slug);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreateArticleCommand(
    string   Title,
    string   Slug,
    string   Content,
    string?  Excerpt,
    string?  CoverImageUrl,
    string   Author,
    string[] Tags,
    bool     PublishImmediately = false
) : IRequest<Guid>;

public sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public CreateArticleCommandValidator(ILeagueDbContext db)
    {
        _db = db;

        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Author).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(300)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case, e.g. 'idiski-wins-derby'.")
            .MustAsync(async (slug, ct) =>
                !await db.Articles.AnyAsync(a => a.Slug == slug, ct))
            .WithMessage("Slug already exists.");
    }
}

public sealed class CreateArticleCommandHandler
    : IRequestHandler<CreateArticleCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        CreateArticleCommand request,
        CancellationToken cancellationToken)
    {
        var article = new Article
        {
            Title         = request.Title,
            Slug          = request.Slug,
            Content       = request.Content,
            Excerpt       = request.Excerpt,
            CoverImageUrl = request.CoverImageUrl,
            Author        = request.Author,
            Tags          = request.Tags,
            IsPublished   = request.PublishImmediately,
            PublishedAt   = request.PublishImmediately ? DateTime.UtcNow : null
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);

        return article.Id;
    }
}

/// <summary>Publishes a draft article and stamps PublishedAt.</summary>
public sealed record PublishArticleCommand(Guid Id) : IRequest;

public sealed class PublishArticleCommandHandler : IRequestHandler<PublishArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public PublishArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(PublishArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Article), request.Id);

        if (article.IsPublished)
            throw new InvalidOperationException("Article is already published.");

        article.IsPublished = true;
        article.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
