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
    /// Admin lists only: the tagged player has left the team this was made about, so it is
    /// now that team's record of their time there and the editor should not offer it.
    /// </summary>
    bool      IsLocked = false
);
