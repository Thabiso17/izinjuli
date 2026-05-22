using iDiski.Domain.Entities;
using FluentValidation;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Articles.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// COMMAND
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Creates a new article. The URL slug is auto-generated from <see cref="Title"/>
/// — callers must NOT supply a slug. Collisions are resolved automatically by
/// appending "-2", "-3", etc.
/// </summary>
public sealed record CreateArticleCommand(
    /// <summary>Article headline. Drives the auto-generated SEO slug.</summary>
    string Title,

    /// <summary>Full body content — store as Markdown; Angular renders it.</summary>
    string Content,

    /// <summary>
    /// Short teaser shown on cards/lists (max 300 chars).
    /// Auto-truncated from Content if not supplied.
    /// </summary>
    string? Excerpt,

    string? CoverImageUrl,

    /// <summary>YouTube video ID or embed URL for rich media content.</summary>
    string? VideoUrl,

    /// <summary>High-quality banner image URL for featured articles.</summary>
    string? FeaturedImageUrl,

    /// <summary>Display name of the author, e.g. "iDiski Editorial Team".</summary>
    string Author,

    /// <summary>
    /// Freeform tags. Stored as PostgreSQL text[].
    /// Recommended values: "Awards", "Player of the Month", "Match Report",
    /// "Transfers", "Midfielders", "Defenders", etc.
    /// </summary>
    string[] Tags,

    /// <summary>When true, sets IsPublished and PublishedAt immediately on creation.</summary>
    bool PublishImmediately = false

) : IRequest<CreateArticleResult>;

// ── Result ────────────────────────────────────────────────────────────────────

/// <summary>Returns both the new ID and the generated slug so the caller
/// can build the canonical URL without a second round-trip.</summary>
public sealed record CreateArticleResult(Guid Id, string Slug);

// ═════════════════════════════════════════════════════════════════════════════
// VALIDATOR
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("Article content must be at least 20 characters.");

        RuleFor(x => x.Author)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Excerpt)
            .MaximumLength(500)
            .When(x => x.Excerpt is not null);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Length <= 10)
            .WithMessage("A maximum of 10 tags are allowed per article.")
            .Must(tags => tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must be 50 characters or fewer.");

        RuleFor(x => x.VideoUrl)
            .MaximumLength(500)
            .Must(BeValidYouTubeUrlOrId)
            .WithMessage("VideoUrl must be a valid YouTube video ID (11 characters) or YouTube URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));

        RuleFor(x => x.FeaturedImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrl)
            .WithMessage("FeaturedImageUrl must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.FeaturedImageUrl));
    }

    private static bool BeValidYouTubeUrlOrId(string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl)) return true;

        // YouTube video ID format: 11 characters (alphanumeric, underscore, hyphen)
        if (videoUrl.Length == 11 && videoUrl.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return true;

        // YouTube URL formats
        if (Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();
            return host.Contains("youtube.com") || host.Contains("youtu.be");
        }

        return false;
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HANDLER
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CreateArticleCommandHandler
    : IRequestHandler<CreateArticleCommand, CreateArticleResult>
{
    private readonly ILeagueDbContext _db;

    public CreateArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<CreateArticleResult> Handle(
        CreateArticleCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Generate base slug from title ──────────────────────────────────
        var baseSlug = SlugGenerator.Generate(request.Title);

        // ── 2. Resolve collisions: "my-slug", "my-slug-2", "my-slug-3" ... ───
        var slug = baseSlug;
        var attempt = 1;

        while (await _db.Articles.AnyAsync(
                   a => a.Slug == slug, cancellationToken))
        {
            attempt++;
            slug = SlugGenerator.WithSuffix(baseSlug, attempt);
        }

        // ── 3. Auto-generate excerpt if omitted (first 300 chars of content) ──
        var excerpt = request.Excerpt
                      ?? (request.Content.Length > 300
                          ? request.Content[..300].TrimEnd() + "…"
                          : request.Content);

        // ── 4. Normalise tags: trim whitespace, deduplicate, sort ─────────────
        var normalisedTags = request.Tags
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToArray();

        // ── 5. Persist ────────────────────────────────────────────────────────
        var article = new Article
        {
            Title            = request.Title.Trim(),
            Slug             = slug,
            Content          = request.Content,
            Excerpt          = excerpt,
            CoverImageUrl    = request.CoverImageUrl,
            VideoUrl         = request.VideoUrl,
            FeaturedImageUrl = request.FeaturedImageUrl,
            Author           = request.Author.Trim(),
            Tags             = normalisedTags,
            IsPublished      = request.PublishImmediately,
            PublishedAt      = request.PublishImmediately ? DateTime.UtcNow : null
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateArticleResult(article.Id, article.Slug);
    }
}
