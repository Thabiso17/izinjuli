namespace iDiski.Application.Articles;

public sealed record ArticleAttachmentDto(
    Guid   Id,
    string FileName,
    string FileUrl,
    string Type,
    long   FileSizeBytes,
    string? Caption,
    int    DisplayOrder
);

public sealed record AddArticleAttachmentCommand(
    Guid ArticleId,
    string FileName,
    string FileUrl,
    string Type,
    long FileSizeBytes,
    string? Caption,
    int DisplayOrder
) : MediatR.IRequest<Guid>;

public sealed record RemoveArticleAttachmentCommand(
    Guid Id
) : MediatR.IRequest<MediatR.Unit>;
