using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using MediatR;

namespace iDiski.Application.Articles.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// DELETE
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Hard-deletes an article. Only unpublished articles may be deleted.
/// To retract a published article, call <see cref="UnpublishArticleCommand"/> first.
/// </summary>
public sealed record DeleteArticleCommand(Guid Id) : IRequest;

public sealed class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public DeleteArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(iDiski.Domain.Entities.Article), request.Id);

        if (article.IsPublished)
            throw new InvalidOperationException(
                "Published articles cannot be deleted directly. " +
                "Call PATCH /unpublish first to retract it, then delete.");

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UNPUBLISH  (retract a live article back to draft)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record UnpublishArticleCommand(Guid Id) : IRequest;

public sealed class UnpublishArticleCommandHandler : IRequestHandler<UnpublishArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public UnpublishArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UnpublishArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(iDiski.Domain.Entities.Article), request.Id);

        if (!article.IsPublished)
            throw new InvalidOperationException("Article is already a draft.");

        article.IsPublished = false;
        // Keep PublishedAt so there is an audit trail of when it was live

        await _db.SaveChangesAsync(cancellationToken);
    }
}
