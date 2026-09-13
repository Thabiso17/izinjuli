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

        // PublishedAt survives unpublishing, so it records that this was live at some point.
        // Once that has happened the article is part of the league's record: archive it.
        if (article.PublishedAt is not null)
            throw new InvalidOperationException(
                "This article has been published, so it can no longer be deleted. "
                + "Archive it instead to retire it from public view.");

        if (article.IsArchived)
            throw new InvalidOperationException("Archived articles cannot be deleted.");

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

// ═════════════════════════════════════════════════════════════════════════════
// ARCHIVE / UNARCHIVE  (retire from public view without destroying the record)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record ArchiveArticleCommand(Guid Id) : IRequest;

public sealed class ArchiveArticleCommandHandler : IRequestHandler<ArchiveArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public ArchiveArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(ArchiveArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(iDiski.Domain.Entities.Article), request.Id);

        if (article.IsArchived)
            throw new InvalidOperationException("Article is already archived.");

        article.IsArchived = true;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record UnarchiveArticleCommand(Guid Id) : IRequest;

public sealed class UnarchiveArticleCommandHandler : IRequestHandler<UnarchiveArticleCommand>
{
    private readonly ILeagueDbContext _db;

    public UnarchiveArticleCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UnarchiveArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(iDiski.Domain.Entities.Article), request.Id);

        if (!article.IsArchived)
            throw new InvalidOperationException("Article is not archived.");

        article.IsArchived = false;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
