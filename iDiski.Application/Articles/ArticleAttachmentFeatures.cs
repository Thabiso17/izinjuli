using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Articles;

// ══════════════════════════════════════════════════════════════════════════════
// ADD ATTACHMENT
// ══════════════════════════════════════════════════════════════════════════════

public sealed class AddArticleAttachmentCommandHandler
    : IRequestHandler<AddArticleAttachmentCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public AddArticleAttachmentCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        AddArticleAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify article exists
        var articleExists = await _db.Articles
            .AnyAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (!articleExists)
            throw new KeyNotFoundException($"Article with ID {request.ArticleId} not found.");

        var attachment = new ArticleAttachment
        {
            ArticleId = request.ArticleId,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            Type = Enum.Parse<AttachmentType>(request.Type),
            FileSizeBytes = request.FileSizeBytes,
            Caption = request.Caption,
            DisplayOrder = request.DisplayOrder
        };

        _db.ArticleAttachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);

        return attachment.Id;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// REMOVE ATTACHMENT
// ══════════════════════════════════════════════════════════════════════════════

public sealed class RemoveArticleAttachmentCommandHandler
    : IRequestHandler<RemoveArticleAttachmentCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public RemoveArticleAttachmentCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        RemoveArticleAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var attachment = await _db.ArticleAttachments
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (attachment == null)
            throw new KeyNotFoundException($"Attachment with ID {request.Id} not found.");

        _db.ArticleAttachments.Remove(attachment);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET ATTACHMENTS BY ARTICLE
// ══════════════════════════════════════════════════════════════════════════════

public sealed record GetArticleAttachmentsQuery(Guid ArticleId)
    : IRequest<IReadOnlyList<ArticleAttachmentDto>>;

public sealed class GetArticleAttachmentsQueryHandler
    : IRequestHandler<GetArticleAttachmentsQuery, IReadOnlyList<ArticleAttachmentDto>>
{
    private readonly ILeagueDbContext _db;

    public GetArticleAttachmentsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<ArticleAttachmentDto>> Handle(
        GetArticleAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.ArticleAttachments
            .AsNoTracking()
            .Where(a => a.ArticleId == request.ArticleId)
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new ArticleAttachmentDto(
                a.Id,
                a.FileName,
                a.FileUrl,
                a.Type.ToString(),
                a.FileSizeBytes,
                a.Caption,
                a.DisplayOrder))
            .ToListAsync(cancellationToken);
    }
}
