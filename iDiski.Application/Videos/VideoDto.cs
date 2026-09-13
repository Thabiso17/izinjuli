namespace iDiski.Application.Videos;

public sealed record VideoDto(
    Guid      Id,
    string    Title,
    string    VideoUrl,
    string?   Description,
    string?   ThumbnailUrl,
    string    Author,
    bool      IsPublished,
    DateTime? PublishedAt,
    bool      IsPinned,
    int       ViewCount,
    Guid?     DivisionId = null,
    Guid?     TeamId = null,
    Guid?     PlayerId = null
);

public sealed record VideoSummaryDto(
    Guid      Id,
    string    Title,
    string    VideoUrl,
    string?   Description,
    string?   ThumbnailUrl,
    string    Author,
    DateTime? PublishedAt,
    bool      IsPinned,

    /// <summary>
    /// Retired from public view but kept on record. Only ever true in admin listings.
    /// </summary>
    bool      IsArchived = false
);
