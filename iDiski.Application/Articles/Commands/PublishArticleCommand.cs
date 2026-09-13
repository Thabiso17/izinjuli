using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using MediatR;

namespace iDiski.Application.Articles.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// PUBLISH  (draft → live)
// ═════════════════════════════════════════════════════════════════════════════

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

        if (article.IsArchived)
            throw new InvalidOperationException(
                "This article is archived. Restore it from the archive to publish it again.");

        article.IsPublished = true;
        article.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
