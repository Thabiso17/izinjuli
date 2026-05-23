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
    int       ViewCount
);

public sealed record VideoSummaryDto(
    Guid      Id,
    string    Title,
    string    VideoUrl,
    string?   Description,
    string?   ThumbnailUrl,
    string    Author,
    DateTime? PublishedAt,
    bool      IsPinned
);
