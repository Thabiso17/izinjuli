using FluentValidation;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;

namespace iDiski.Application.Articles.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// COMMAND
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Updates the editable fields of an existing article.
/// The <b>Slug is intentionally immutable</b> — changing it after publish breaks
/// inbound links and SEO. If you need a new slug, unpublish and create a new article.
/// </summary>
public sealed record UpdateArticleCommand(
    Guid     Id,
    string   Title,
    string   Content,
    string?  Excerpt,
    string?  CoverImageUrl,
    string?  VideoUrl,
    string?  FeaturedImageUrl,
    string   Author,
    string[] Tags,
    bool     IsPinned = false
) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(20);

        RuleFor(x => x.Author)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Excerpt)
            .MaximumLength(500)
            .When(x => x.Excerpt is not null);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Length <= 10)
            .WithMessage("A maximum of 10 tags are allowed per article.");

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

        if (videoUrl.Length == 11 && videoUrl.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return true;

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

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdateArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Article), request.Id);

        // ── Slug stays untouched — only content fields update ─────────────────
        article.Title            = request.Title.Trim();
        article.Content          = request.Content;
        article.CoverImageUrl    = request.CoverImageUrl;
        article.VideoUrl         = request.VideoUrl;
        article.FeaturedImageUrl = request.FeaturedImageUrl;
        article.Author           = request.Author.Trim();
        article.IsPinned         = request.IsPinned;
        article.Tags = request.Tags
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToArray();

        // Auto-refresh excerpt only if caller didn't supply one
        article.Excerpt = request.Excerpt
                          ?? (request.Content.Length > 300
                              ? request.Content[..300].TrimEnd() + "…"
                              : request.Content);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
